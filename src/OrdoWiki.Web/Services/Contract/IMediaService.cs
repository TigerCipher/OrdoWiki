namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface IMediaService
{
    Task<ApiResponse<MediaAssetDto>> UploadImageAsync(
        Stream input,
        string originalName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> SaveAvatarAsync(
        Stream input,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);
}
