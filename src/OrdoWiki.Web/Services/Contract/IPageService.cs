namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface IPageService
{
    Task<ApiResponse<WikiPageDto>> GetPageByIdAsync(Guid id);
    Task<ApiResponse<WikiPageDto>> GetPageBySlugAsync(string slug);
    Task<ApiResponse<WikiPageDto>> CreatePageAsync(CreatePageRequest request);
    Task<ApiResponse<WikiPageDto>> EditPageAsync(EditPageRequest request);

    Task<ApiResponse<List<WikiPageDto>>> GetPagesAsync(Guid? tagId = null);
}