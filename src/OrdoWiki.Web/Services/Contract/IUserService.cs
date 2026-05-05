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
}