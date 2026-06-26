namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetCurrentUserAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(string userId);

    /// <summary>
    /// Returns the highest-priority role per user for the supplied IDs.
    /// IDs without a role are omitted from the result.
    /// </summary>
    Task<Dictionary<string, string?>> GetHighestRolesAsync(IEnumerable<string> userIds);

    /// <summary>
    /// Returns users whose username or display name contains <paramref name="query"/>.
    /// Pass null/empty to return up to <paramref name="limit"/> users alphabetically.
    /// </summary>
    Task<List<UserDto>> SearchUsersAsync(string? query, int limit = 20);
}