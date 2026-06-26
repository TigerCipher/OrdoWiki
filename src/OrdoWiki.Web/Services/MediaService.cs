namespace OrdoWiki.Web.Services;

using Data;
using Data.Entities;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        MediaSourceType sourceType = MediaSourceType.Standalone,
        Guid? sourceId = null,
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
                SourceType = sourceType,
                SourceId = sourceId,
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

    public async Task<ApiResponse<MediaAssetDto>> AttachExistingAsync(
        Guid sourceAssetId,
        MediaSourceType sourceType,
        Guid? sourceId,
        CancellationToken cancellationToken = default)
    {
        MediaAsset? source = await context.MediaAssets
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == sourceAssetId, cancellationToken);
        if (source is null) return NotFound<MediaAssetDto>();

        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<MediaAssetDto>(userResponse.Error);

        // Clone every file-level field; just rewrite the attachment (source) and
        // who-attached-it. The physical file is shared via StoragePath.
        MediaAsset clone = new()
        {
            Id = Guid.NewGuid(),
            StoragePath = source.StoragePath,
            OriginalName = source.OriginalName,
            ContentType = source.ContentType,
            SizeBytes = source.SizeBytes,
            Width = source.Width,
            Height = source.Height,
            UploadedById = userResponse.Value.Id,
            UploadedAt = DateTime.UtcNow,
            SourceType = sourceType,
            SourceId = sourceId,
        };
        context.MediaAssets.Add(clone);

        // Copy tag joins so the clone inherits the source's tags. They can
        // diverge after.
        List<Guid> tagIds = await context.MediaAssetTags
            .AsNoTracking()
            .Where(j => j.MediaAssetId == sourceAssetId)
            .Select(j => j.TagId)
            .ToListAsync(cancellationToken);

        foreach (Guid tagId in tagIds)
            context.MediaAssetTags.Add(new MediaAssetTag { MediaAssetId = clone.Id, TagId = tagId });

        await context.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(clone));
    }

    public async Task DeleteAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        // Stay off the change tracker entirely. The Blazor Server circuit
        // shares one scoped DbContext across user actions, so a CharacterImage
        // that got tracked earlier (e.g., from the picker's Add+SaveChanges)
        // can still be Unchanged in memory even after we ExecuteDelete its row.
        // Calling Remove(asset) would then trigger EF's cascade check against
        // those stale tracked dependents and throw on the required FK.
        string? storagePath = await context.MediaAssets
            .AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => a.StoragePath)
            .SingleOrDefaultAsync(cancellationToken);
        if (storagePath is null) return;

        // CharacterImage FK is Restrict, so the joins have to go before the
        // asset row. Tags Cascade in theory but we clean them up explicitly
        // for symmetry.
        await context.MediaAssetTags
            .Where(j => j.MediaAssetId == assetId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.CharacterImages
            .Where(i => i.MediaAssetId == assetId)
            .ExecuteDeleteAsync(cancellationToken);

        await context.MediaAssets
            .Where(a => a.Id == assetId)
            .ExecuteDeleteAsync(cancellationToken);

        // Detach any now-stale tracker entries so a later SaveChanges in this
        // circuit doesn't fail trying to fixup relationships to deleted rows.
        foreach (EntityEntry entry in context.ChangeTracker.Entries()
            .Where(e =>
                (e.Entity is MediaAsset m && m.Id == assetId)
                || (e.Entity is CharacterImage c && c.MediaAssetId == assetId)
                || (e.Entity is MediaAssetTag t && t.MediaAssetId == assetId))
            .ToList())
        {
            entry.State = EntityState.Detached;
        }

        // Only blow away the file if no surviving MediaAsset still references it
        // (picker-cloned attachments share a StoragePath).
        bool stillReferenced = await context.MediaAssets
            .AsNoTracking()
            .AnyAsync(a => a.StoragePath == storagePath, cancellationToken);
        if (!stillReferenced) TryDeleteFile(storagePath);
    }

    public void TryDeleteFile(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath)) return;

        // StoragePath is a web path like "/uploads/2026/06/abc123.webp". Strip the
        // "/uploads/" prefix and resolve against UploadsRoot. Refuse anything that
        // escapes the root after canonicalization — defends against a malformed row.
        const string prefix = "/uploads/";
        if (!storagePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

        string relative = storagePath[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        string root = Path.GetFullPath(UploadsRoot);
        string target = Path.GetFullPath(Path.Combine(root, relative));

        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            if (File.Exists(target)) File.Delete(target);
        }
        catch (IOException) { /* leave the orphan — the DB row is gone */ }
        catch (UnauthorizedAccessException) { }
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
