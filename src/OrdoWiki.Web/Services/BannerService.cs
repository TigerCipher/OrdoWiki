namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Models.Requests;
using System.Security.Claims;

public class BannerService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IUserService userService,
    BannerState bannerState,
    AuthenticationStateProvider authState,
    IAuthorizationService authorization) : IBannerService
{
    private const int AdminSlot = 4;

    public async Task<IReadOnlyList<BannerDto>> GetSlotsAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        List<Banner> banners = await context.Banners
            .AsNoTracking()
            .Include(b => b.MediaAsset)
            .OrderBy(b => b.SlotIndex)
            .ToListAsync();

        return banners.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<BannerDto>> GetVisibleAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        List<Banner> banners = await context.Banners
            .AsNoTracking()
            .Include(b => b.MediaAsset)
            .Where(b => b.MediaAssetId != null)
            .OrderBy(b => b.SlotIndex)
            .ToListAsync();

        return banners.Select(MapToDto).ToList();
    }

    public async Task<ApiResponse<BannerDto>> SetAsync(SetBannerRequest request)
    {
        if (request.SlotIndex < 1 || request.SlotIndex > 4)
            return BadRequest<BannerDto>("Slot must be between 1 and 4.");

        AuthenticationState state = await authState.GetAuthenticationStateAsync();
        ClaimsPrincipal user = state.User;

        // Slot 4 is admin-reserved. Slots 1-3 need any CanDesign user.
        bool requiresAdmin = request.SlotIndex == AdminSlot;
        if (requiresAdmin)
        {
            if (!user.IsInRole(Roles.Admin))
                return Forbidden<BannerDto>("Slot 4 is reserved for administrators.");
        }
        else
        {
            AuthorizationResult result = await authorization.AuthorizeAsync(user, Policies.CanDesign);
            if (!result.Succeeded)
                return Forbidden<BannerDto>("You don't have permission to edit banners.");
        }

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        Banner? banner = await context.Banners.SingleOrDefaultAsync(b => b.SlotIndex == request.SlotIndex);
        if (banner is null) return NotFound<BannerDto>();

        if (request.MediaAssetId.HasValue)
        {
            bool exists = await context.MediaAssets.AnyAsync(a => a.Id == request.MediaAssetId.Value);
            if (!exists) return BadRequest<BannerDto>("Selected image does not exist.");
        }

        ApiResponse<UserDto> me = await userService.GetCurrentUserAsync();

        banner.MediaAssetId = request.MediaAssetId;
        banner.Alt = string.IsNullOrWhiteSpace(request.Alt) ? null : request.Alt!.Trim();
        banner.LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl!.Trim();
        banner.UpdatedAt = DateTime.UtcNow;
        banner.UpdatedById = me.Success ? me.Value.Id : null;

        await context.SaveChangesAsync();

        Banner refreshed = await context.Banners
            .AsNoTracking()
            .Include(b => b.MediaAsset)
            .SingleAsync(b => b.Id == banner.Id);

        await bannerState.NotifyChangedAsync();

        return Ok(MapToDto(refreshed));
    }

    private static BannerDto MapToDto(Banner b) => new()
    {
        Id = b.Id,
        SlotIndex = b.SlotIndex,
        MediaAssetId = b.MediaAssetId,
        ImageUrl = b.MediaAsset?.StoragePath,
        Alt = b.Alt,
        LinkUrl = b.LinkUrl,
        UpdatedAt = b.UpdatedAt,
    };
}
