namespace OrdoWiki.Web.Services;

using System.Security.Claims;
using Contract;
using Data;
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

        return user is null
            ? NotFound<UserDto>()
            : Ok(MapToDto(user));
    }
}