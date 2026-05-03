namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetCurrentUserAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(string userId);
}