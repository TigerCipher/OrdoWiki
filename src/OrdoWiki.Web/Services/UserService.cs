namespace OrdoWiki.Web.Services;

using System.Security.Claims;
using Contract;
using Data;
using Data.Auth;
using Data.Entities;
using Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Models;

public class UserService(
    ApplicationDbContext context,
    AuthenticationStateProvider authProvider) : IUserService
{
    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync()
    {
        AuthenticationState auth = await authProvider.GetAuthenticationStateAsync();
        ClaimsPrincipal claims = auth.User;
        if (claims.Identity?.IsAuthenticated != true)
            return Unauthorized<UserDto>("User not signed in");

        string? userId = claims.GetUserId();
        if (userId is null)
            return Unauthorized<UserDto>("User ID not found in claims");

        return await GetUserByIdAsync(userId);
    }

    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(string userId)
    {
        ApplicationUser? user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound<UserDto>();

        Dictionary<string, string?> roles = await GetHighestRolesAsync([userId]);
        roles.TryGetValue(userId, out string? role);

        return Ok(MapToDto(user, role));
    }

    public async Task<Dictionary<string, string?>> GetHighestRolesAsync(IEnumerable<string> userIds)
    {
        HashSet<string> ids = [..userIds];
        if (ids.Count == 0) return new Dictionary<string, string?>();

        List<UserRoleRow> rows = await context.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new UserRoleRow(ur.UserId, r.Name))
            .ToListAsync();

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => Roles.PickHighest(g.Select(x => x.RoleName)));
    }

    public async Task<List<UserDto>> SearchUsersAsync(string? query, int limit = 20)
    {
        IQueryable<ApplicationUser> q = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            string trimmed = query.Trim();
            q = q.Where(u =>
                (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{trimmed}%"))
                || EF.Functions.ILike(u.UserName!, $"%{trimmed}%"));
        }

        List<ApplicationUser> users = await q
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync();

        Dictionary<string, string?> roles = await GetHighestRolesAsync(users.Select(u => u.Id));
        return users.Select(u => MapToDto(u, roles.GetValueOrDefault(u.Id))).ToList();
    }

    private record UserRoleRow(string UserId, string? RoleName);
}