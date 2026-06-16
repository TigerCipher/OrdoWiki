namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface IGalleryService
{
    Task<ApiResponse<PagedResult<GalleryItemDto>>> GetGalleryAsync(GalleryFilter filter);

    Task<ApiResponse<List<UserDto>>> GetUploadersAsync();

    Task<ApiResponse<bool>> DeleteAsync(Guid assetId);
}
