namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Models.Requests;
using System.Text.Json;

public class SiteThemeService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IUserService userService,
    SiteThemeState state,
    AuthenticationStateProvider authState,
    IAuthorizationService authorization) : ISiteThemeService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<SiteThemeDto> GetAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        SiteTheme? row = await context.SiteThemes
            .AsNoTracking()
            .Include(t => t.LightBackgroundAsset)
            .Include(t => t.DarkBackgroundAsset)
            .SingleOrDefaultAsync(t => t.Id == SiteTheme.SingletonId);

        if (row is null) return new SiteThemeDto();

        return new SiteThemeDto
        {
            LightPalette = ParseDict(row.LightPaletteJson),
            DarkPalette = ParseDict(row.DarkPaletteJson),
            CustomValues = ParseCustomValues(row.CustomValuesJson),
            LightBackgroundAssetId = row.LightBackgroundAssetId,
            LightBackgroundUrl = row.LightBackgroundAsset?.StoragePath,
            DarkBackgroundAssetId = row.DarkBackgroundAssetId,
            DarkBackgroundUrl = row.DarkBackgroundAsset?.StoragePath,
        };
    }

    public async Task<IReadOnlyList<CustomThemeVariableDto>> GetCustomVariablesAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.CustomThemeVariables
            .AsNoTracking()
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.Name)
            .Select(v => new CustomThemeVariableDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                SortOrder = v.SortOrder,
            })
            .ToListAsync();
    }

    public async Task<ApiResponse<SiteThemeDto>> SaveAsync(SaveSiteThemeRequest request)
    {
        ApiResponse<bool> auth = await EnsureCanDesignAsync<bool>();
        if (!auth.Success) return Forbidden<SiteThemeDto>(auth.Error);

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        SiteTheme? row = await context.SiteThemes.SingleOrDefaultAsync(t => t.Id == SiteTheme.SingletonId);
        if (row is null)
        {
            row = new SiteTheme { Id = SiteTheme.SingletonId };
            context.SiteThemes.Add(row);
        }

        if (request.LightBackgroundAssetId.HasValue
            && !await context.MediaAssets.AnyAsync(a => a.Id == request.LightBackgroundAssetId.Value))
            return BadRequest<SiteThemeDto>("Light background image does not exist.");

        if (request.DarkBackgroundAssetId.HasValue
            && !await context.MediaAssets.AnyAsync(a => a.Id == request.DarkBackgroundAssetId.Value))
            return BadRequest<SiteThemeDto>("Dark background image does not exist.");

        ApiResponse<UserDto> me = await userService.GetCurrentUserAsync();

        row.LightPaletteJson = JsonSerializer.Serialize(request.LightPalette ?? new(), JsonOpts);
        row.DarkPaletteJson = JsonSerializer.Serialize(request.DarkPalette ?? new(), JsonOpts);
        row.CustomValuesJson = JsonSerializer.Serialize(request.CustomValues ?? new(), JsonOpts);
        row.LightBackgroundAssetId = request.LightBackgroundAssetId;
        row.DarkBackgroundAssetId = request.DarkBackgroundAssetId;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedById = me.Success ? me.Value.Id : null;

        await context.SaveChangesAsync();
        await state.NotifyChangedAsync();

        return Ok(await GetAsync());
    }

    public async Task<ApiResponse<CustomThemeVariableDto>> CreateCustomVariableAsync(CreateCustomVariableRequest request)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync<bool>();
        if (!auth.Success) return Forbidden<CustomThemeVariableDto>(auth.Error);

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest<CustomThemeVariableDto>("Name is required.");

        string normalized = NormalizeVarName(request.Name);

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        if (await context.CustomThemeVariables.AnyAsync(v => v.Name == normalized))
            return BadRequest<CustomThemeVariableDto>($"A variable named '{normalized}' already exists.");

        CustomThemeVariable v = new()
        {
            Id = Guid.NewGuid(),
            Name = normalized,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim(),
            SortOrder = request.SortOrder,
        };

        context.CustomThemeVariables.Add(v);
        await context.SaveChangesAsync();

        return Ok(new CustomThemeVariableDto
        {
            Id = v.Id, Name = v.Name, Description = v.Description, SortOrder = v.SortOrder,
        });
    }

    public async Task<ApiResponse<CustomThemeVariableDto>> UpdateCustomVariableAsync(UpdateCustomVariableRequest request)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync<bool>();
        if (!auth.Success) return Forbidden<CustomThemeVariableDto>(auth.Error);

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        CustomThemeVariable? v = await context.CustomThemeVariables.SingleOrDefaultAsync(x => x.Id == request.Id);
        if (v is null) return NotFound<CustomThemeVariableDto>();

        string normalized = NormalizeVarName(request.Name);
        if (normalized != v.Name && await context.CustomThemeVariables.AnyAsync(x => x.Name == normalized))
            return BadRequest<CustomThemeVariableDto>($"A variable named '{normalized}' already exists.");

        v.Name = normalized;
        v.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description!.Trim();
        v.SortOrder = request.SortOrder;

        await context.SaveChangesAsync();

        return Ok(new CustomThemeVariableDto
        {
            Id = v.Id, Name = v.Name, Description = v.Description, SortOrder = v.SortOrder,
        });
    }

    public async Task<ApiResponse<bool>> DeleteCustomVariableAsync(Guid id)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync<bool>();
        if (!auth.Success) return auth;

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        CustomThemeVariable? v = await context.CustomThemeVariables.SingleOrDefaultAsync(x => x.Id == id);
        if (v is null) return NotFound<bool>();

        context.CustomThemeVariables.Remove(v);
        await context.SaveChangesAsync();
        return Ok(true);
    }

    private static string NormalizeVarName(string name)
    {
        string trimmed = name.Trim();
        return trimmed.StartsWith("--", StringComparison.Ordinal) ? trimmed : "--" + trimmed;
    }

    private static Dictionary<string, string> ParseDict(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOpts) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static Dictionary<string, ThemeValuePair> ParseCustomValues(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, ThemeValuePair>>(json, JsonOpts) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task<ApiResponse<T>> EnsureCanDesignAsync<T>()
    {
        AuthenticationState s = await authState.GetAuthenticationStateAsync();
        AuthorizationResult result = await authorization.AuthorizeAsync(s.User, Policies.CanDesign);
        return result.Succeeded ? Ok(default(T)!) : Forbidden<T>("You don't have permission to edit the theme.");
    }

    private async Task<ApiResponse<T>> EnsureAdminAsync<T>()
    {
        AuthenticationState s = await authState.GetAuthenticationStateAsync();
        return s.User.IsInRole(Roles.Admin)
            ? Ok(default(T)!)
            : Forbidden<T>("Only admins can manage custom theme variables.");
    }
}
