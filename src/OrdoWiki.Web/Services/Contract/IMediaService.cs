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
}
