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

    void TryDeleteFile(string storagePath);
}
