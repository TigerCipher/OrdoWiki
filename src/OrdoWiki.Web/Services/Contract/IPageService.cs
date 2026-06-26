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

    /// <summary>
    /// Returns all revisions for a page, newest first. Bodies are excluded — use
    /// <see cref="GetRevisionAsync"/> to load a specific revision's content.
    /// </summary>
    Task<ApiResponse<List<PageRevisionDto>>> GetRevisionsAsync(Guid pageId);

    /// <summary>
    /// Returns a single revision including its full markdown body.
    /// </summary>
    Task<ApiResponse<PageRevisionDto>> GetRevisionAsync(Guid revisionId);

    /// <summary>
    /// Writes a new revision whose body matches the supplied historical revision and
    /// points the page at it. Attribution and timestamp are the restoring user.
    /// </summary>
    Task<ApiResponse<WikiPageDto>> RestoreRevisionAsync(Guid revisionId);
}