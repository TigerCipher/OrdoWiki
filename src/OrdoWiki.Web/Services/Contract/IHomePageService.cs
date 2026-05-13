namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface IHomePageService
{
    Task<HomePageDto> GetAsync();
    Task<ApiResponse<HomePageDto>> UpdateBioAsync(UpdateBioRequest request);
    Task<ApiResponse<HomePageDto>> SetFeaturedLogAsync(SetFeaturedLogRequest request);
}
