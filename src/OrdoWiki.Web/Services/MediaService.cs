namespace OrdoWiki.Web.Services;

using Data;
using Data.Entities;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

public class MediaService(
    ApplicationDbContext context,
    IUserService userService,
    IConfiguration configuration,
    IWebHostEnvironment environment) : IMediaService
{
    private const int MaxDimension = 2400;
    private const int AvatarSize = 256;

    private static readonly HashSet<string> _allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    public async Task<ApiResponse<MediaAssetDto>> UploadImageAsync(
        Stream input,
        string originalName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (sizeBytes <= 0)
            return BadRequest<MediaAssetDto>("File is empty.");

        if (sizeBytes > MediaLimits.MaxImageBytes)
            return BadRequest<MediaAssetDto>($"File is larger than the {MediaLimits.MaxImageBytes / (1024 * 1024)} MB limit.");

        if (!_allowedContentTypes.Contains(contentType))
            return BadRequest<MediaAssetDto>($"Content type '{contentType}' is not allowed.");

        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse)
            return BadRequest<MediaAssetDto>($"User not found - {userResponse.Error}");

        UserDto user = userResponse;

        try
        {
            using Image image = await Image.LoadAsync(input, cancellationToken);

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension),
                }));
            }

            DateTime now = DateTime.UtcNow;
            string id = Guid.NewGuid().ToString("N")[..12];
            string relativePath = $"{now:yyyy}/{now:MM}/{id}.webp";
            string absolutePath = Path.Combine(UploadsRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            await image.SaveAsWebpAsync(absolutePath, new WebpEncoder { Quality = 85 }, cancellationToken);

            FileInfo info = new(absolutePath);

            MediaAsset asset = new()
            {
                Id = Guid.NewGuid(),
                StoragePath = $"/uploads/{relativePath.Replace('\\', '/')}",
                OriginalName = string.IsNullOrWhiteSpace(originalName) ? "upload" : Path.GetFileName(originalName),
                ContentType = "image/webp",
                SizeBytes = info.Length,
                Width = image.Width,
                Height = image.Height,
                UploadedById = user.Id,
                UploadedAt = now,
            };

            context.MediaAssets.Add(asset);
            await context.SaveChangesAsync(cancellationToken);

            return Ok(MapToDto(asset));
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest<MediaAssetDto>("That file isn't a supported image.");
        }
        catch (InvalidImageContentException)
        {
            return BadRequest<MediaAssetDto>("That image file appears to be corrupted.");
        }
    }

    public async Task<ApiResponse<string>> SaveAvatarAsync(
        Stream input,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        if (sizeBytes <= 0)
            return BadRequest<string>("File is empty.");

        if (sizeBytes > MediaLimits.MaxAvatarBytes)
            return BadRequest<string>($"Image is larger than the {MediaLimits.MaxAvatarBytes / (1024 * 1024)} MB limit.");

        if (!_allowedContentTypes.Contains(contentType))
            return BadRequest<string>($"Content type '{contentType}' is not allowed.");

        try
        {
            using Image image = await Image.LoadAsync(input, cancellationToken);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Size = new Size(AvatarSize, AvatarSize),
            }));

            string id = Guid.NewGuid().ToString("N")[..12];
            string relativePath = $"avatars/{id}.webp";
            string absolutePath = Path.Combine(UploadsRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

            await image.SaveAsWebpAsync(absolutePath, new WebpEncoder { Quality = 80 }, cancellationToken);

            return Ok($"/uploads/{relativePath}");
        }
        catch (UnknownImageFormatException)
        {
            return BadRequest<string>("That file isn't a supported image.");
        }
        catch (InvalidImageContentException)
        {
            return BadRequest<string>("That image file appears to be corrupted.");
        }
    }

    private string UploadsRoot
    {
        get
        {
            string root = configuration["UploadsRoot"]
                ?? Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
