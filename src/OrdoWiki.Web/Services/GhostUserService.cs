namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models.Requests;

public class GhostUserService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IUserService userService) : IGhostUserService
{
    public async Task<ApiResponse<UserDto>> CreateAsync(CreateGhostUserRequest request)
    {
        ApiResponse<bool> auth = await EnsureAdminOrDesignerAsync();
        if (!auth.Success) return Unauthorized<UserDto>(auth.Error);

        string displayName = request.DisplayName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(displayName))
            return BadRequest<UserDto>("Display name is required.");

        // Ghost usernames have to be unique for Identity but the user can't sign
        // in with them, so a random suffix is fine.
        string username = $"ghost-{Guid.NewGuid():N}"[..14];

        ApplicationUser ghost = new()
        {
            UserName = username,
            Email = $"{username}@ghost.local",
            EmailConfirmed = false,
            DisplayName = displayName,
            IsGhost = true,
        };

        IdentityResult result = await userManager.CreateAsync(ghost);
        if (!result.Succeeded)
            return BadRequest<UserDto>(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Ok(MapToDto(ghost));
    }

    public async Task<ApiResponse<List<UserDto>>> ListAsync()
    {
        ApiResponse<bool> auth = await EnsureAdminOrDesignerAsync();
        if (!auth.Success) return Unauthorized<List<UserDto>>(auth.Error);

        List<ApplicationUser> ghosts = await context.Users
            .AsNoTracking()
            .Where(u => u.IsGhost)
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .ToListAsync();

        return Ok(ghosts.Select(g => MapToDto(g)).ToList());
    }

    public async Task<ApiResponse<bool>> LinkAsync(LinkGhostUserRequest request)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync();
        if (!auth.Success) return Unauthorized<bool>(auth.Error);

        ApplicationUser? ghost = await context.Users.SingleOrDefaultAsync(u => u.Id == request.GhostId);
        if (ghost is null) return NotFound<bool>();
        if (!ghost.IsGhost) return BadRequest<bool>("Source user is not a ghost.");

        ApplicationUser? real = await context.Users.SingleOrDefaultAsync(u => u.Id == request.RealUserId);
        if (real is null) return BadRequest<bool>("Target user does not exist.");
        if (real.IsGhost) return BadRequest<bool>("Cannot link a ghost to another ghost.");
        if (real.Id == ghost.Id) return BadRequest<bool>("Cannot link a ghost to itself.");

        // Reassign every FK that can point at a user-created entity. Wrap in a
        // transaction so a partial failure doesn't leave dangling references.
        await using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Characters
                .Where(c => c.OwnerId == ghost.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.OwnerId, real.Id));

            await context.WikiPages
                .Where(p => p.CreatedById == ghost.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CreatedById, real.Id));

            await context.PageRevisions
                .Where(r => r.EditedById == ghost.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.EditedById, real.Id));

            await context.TimelineEvents
                .Where(e => e.CreatedById == ghost.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.CreatedById, real.Id));

            await context.MediaAssets
                .Where(a => a.UploadedById == ghost.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.UploadedById, real.Id));

            IdentityResult result = await userManager.DeleteAsync(ghost);
            if (!result.Succeeded)
            {
                await tx.RollbackAsync();
                return BadRequest<bool>($"Failed to delete ghost: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }

            await tx.CommitAsync();
            return Ok(true);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<bool>> DeleteAsync(string ghostId)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync();
        if (!auth.Success) return Unauthorized<bool>(auth.Error);

        ApplicationUser? ghost = await context.Users.SingleOrDefaultAsync(u => u.Id == ghostId);
        if (ghost is null) return NotFound<bool>();
        if (!ghost.IsGhost) return BadRequest<bool>("Only ghost users can be deleted here.");

        // Blocks the delete if the ghost still owns content — prevents an admin
        // from silently deleting a friend's draft characters.
        bool hasContent = await context.Characters.AnyAsync(c => c.OwnerId == ghost.Id)
                       || await context.WikiPages.AnyAsync(p => p.CreatedById == ghost.Id)
                       || await context.TimelineEvents.AnyAsync(e => e.CreatedById == ghost.Id)
                       || await context.MediaAssets.AnyAsync(a => a.UploadedById == ghost.Id);
        if (hasContent)
            return BadRequest<bool>("This ghost still owns content. Link it to a real user first, or reassign manually.");

        IdentityResult result = await userManager.DeleteAsync(ghost);
        return result.Succeeded
            ? Ok(true)
            : BadRequest<bool>(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private async Task<ApiResponse<bool>> EnsureAdminOrDesignerAsync()
    {
        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<bool>(userResponse.Error);

        string? role = userResponse.Value.Role;
        bool ok = string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
               || string.Equals(role, Roles.Designer, StringComparison.OrdinalIgnoreCase);
        return ok ? Ok(true) : Forbidden<bool>("Admin or Designer role required.");
    }

    private async Task<ApiResponse<bool>> EnsureAdminAsync()
    {
        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<bool>(userResponse.Error);

        bool ok = string.Equals(userResponse.Value.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        return ok ? Ok(true) : Forbidden<bool>("Admin role required.");
    }
}
