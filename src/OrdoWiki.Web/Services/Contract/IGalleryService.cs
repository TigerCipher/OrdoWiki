namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface IGalleryService
{
    Task<ApiResponse<PagedResult<GalleryItemDto>>> GetGalleryAsync(GalleryFilter filter);

    Task<ApiResponse<List<UserDto>>> GetUploadersAsync();

    Task<ApiResponse<bool>> DeleteAsync(Guid assetId, bool force = false);

    /// <summary>
    /// Reports what will actually be affected if this asset is deleted: which
    /// characters lose their attachment, and whether the underlying file
    /// goes away (no other MediaAsset still references the same StoragePath).
    /// Independent of the asset's own SourceType — picker cloning can mean a
    /// Standalone row still has CharacterImage joins behind it.
    /// </summary>
    Task<DeleteImpactDto> GetDeleteImpactAsync(Guid assetId);
}
