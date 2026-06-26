namespace OrdoWiki.Web.Services.Contract;

using Data.Entities;
using Models;

public interface IMediaService
{
    Task<ApiResponse<MediaAssetDto>> UploadImageAsync(
        Stream input,
        string originalName,
        string contentType,
        long sizeBytes,
        MediaSourceType sourceType = MediaSourceType.Standalone,
        Guid? sourceId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> SaveAvatarAsync(
        Stream input,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones an existing asset into a new attachment. The new row shares the
    /// same StoragePath (physical file) as the source but carries its own
    /// SourceType/SourceId, so deleting one attachment doesn't take the file
    /// away from the other. Tags are copied to the clone.
    /// </summary>
    Task<ApiResponse<MediaAssetDto>> AttachExistingAsync(
        Guid sourceAssetId,
        MediaSourceType sourceType,
        Guid? sourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the asset row + its tag joins, and ref-count-deletes the file
    /// (only removed from disk if no other MediaAsset still references it).
    /// </summary>
    Task DeleteAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    void TryDeleteFile(string storagePath);
}
