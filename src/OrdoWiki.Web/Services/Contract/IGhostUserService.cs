namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface IGhostUserService
{
    Task<ApiResponse<UserDto>> CreateAsync(CreateGhostUserRequest request);

    Task<ApiResponse<List<UserDto>>> ListAsync();

    Task<ApiResponse<bool>> LinkAsync(LinkGhostUserRequest request);

    Task<ApiResponse<bool>> DeleteAsync(string ghostId);
}
