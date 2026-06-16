namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Entities;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Models;

public class GalleryService(
    ApplicationDbContext context,
    IUserService userService,
    IMediaService mediaService) : IGalleryService
{
    public async Task<ApiResponse<PagedResult<GalleryItemDto>>> GetGalleryAsync(GalleryFilter filter)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        IQueryable<MediaAsset> q = context.MediaAssets
            .AsNoTracking()
            .Include(a => a.UploadedBy)
            .Where(a => a.SourceType != MediaSourceType.Avatar
                     && a.SourceType != MediaSourceType.Banner);

        if (filter.SourceType is { } src)
            q = q.Where(a => a.SourceType == src);

        if (!string.IsNullOrEmpty(filter.UploaderId))
            q = q.Where(a => a.UploadedById == filter.UploaderId);

        if (filter.TagId is { } tagId)
            q = q.Where(a => context.MediaAssetTags.Any(j => j.MediaAssetId == a.Id && j.TagId == tagId));

        int total = await q.CountAsync();

        List<MediaAsset> items = await q
            .OrderByDescending(a => a.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(
            items.Select(a => a.UploadedById));

        Dictionary<Guid, SourceLink> sources = await ResolveSourcesAsync(items);

        List<Guid> assetIds = items.Select(a => a.Id).ToList();
        Dictionary<Guid, List<TagDto>> tagsByAsset = (await context.MediaAssetTags
                .AsNoTracking()
                .Where(j => assetIds.Contains(j.MediaAssetId))
                .Select(j => new { j.MediaAssetId, Tag = new TagDto { Id = j.Tag.Id, Slug = j.Tag.Slug, Name = j.Tag.Name } })
                .ToListAsync())
            .GroupBy(x => x.MediaAssetId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Tag).OrderBy(t => t.Name).ToList());

        List<GalleryItemDto> dtos = items.Select(a => new GalleryItemDto
        {
            Asset = MapToDto(a, roles.GetValueOrDefault(a.UploadedById)),
            Source = a.SourceId.HasValue
                ? sources.GetValueOrDefault(a.SourceId.Value)
                : null,
            Tags = tagsByAsset.GetValueOrDefault(a.Id, []),
        }).ToList();

        return Ok(new PagedResult<GalleryItemDto>(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse<List<UserDto>>> GetUploadersAsync()
    {
        List<ApplicationUser> users = await context.MediaAssets
            .AsNoTracking()
            .Where(a => a.SourceType != MediaSourceType.Avatar
                     && a.SourceType != MediaSourceType.Banner)
            .Select(a => a.UploadedBy!)
            .Distinct()
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(users.Select(u => u.Id));

        return Ok(users.Select(u => MapToDto(u, roles.GetValueOrDefault(u.Id))).ToList());
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid assetId)
    {
        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<bool>(userResponse.Error);

        UserDto user = userResponse;
        if (!IsPrivileged(user.Role))
            return Forbidden<bool>("You don't have permission to delete gallery images.");

        MediaAsset? asset = await context.MediaAssets.SingleOrDefaultAsync(a => a.Id == assetId);
        if (asset is null) return NotFound<bool>();

        // Characters have a dedicated gallery editor — direct gallery delete would
        // bypass the OrderIndex/CharacterImage row management. Avatars and banners
        // are also managed elsewhere. Standalone, WikiPage, and TimelineEvent
        // images have no inline manager, so the gallery is the delete path.
        if (asset.SourceType is MediaSourceType.Character
            or MediaSourceType.Avatar
            or MediaSourceType.Banner)
        {
            return BadRequest<bool>("This image is managed elsewhere — remove it from its source page.");
        }

        await context.MediaAssetTags.Where(j => j.MediaAssetId == assetId).ExecuteDeleteAsync();
        context.MediaAssets.Remove(asset);
        await context.SaveChangesAsync();

        mediaService.TryDeleteFile(asset.StoragePath);
        return Ok(true);
    }

    private static bool IsPrivileged(string? role) =>
        string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Roles.Designer, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Roles.Editor, StringComparison.OrdinalIgnoreCase);

    private async Task<Dictionary<Guid, SourceLink>> ResolveSourcesAsync(IReadOnlyList<MediaAsset> assets)
    {
        Guid[] characterIds = assets
            .Where(a => a.SourceType == MediaSourceType.Character && a.SourceId.HasValue)
            .Select(a => a.SourceId!.Value).Distinct().ToArray();

        Guid[] pageIds = assets
            .Where(a => a.SourceType == MediaSourceType.WikiPage && a.SourceId.HasValue)
            .Select(a => a.SourceId!.Value).Distinct().ToArray();

        Guid[] eventIds = assets
            .Where(a => a.SourceType == MediaSourceType.TimelineEvent && a.SourceId.HasValue)
            .Select(a => a.SourceId!.Value).Distinct().ToArray();

        Dictionary<Guid, SourceLink> map = new();

        if (characterIds.Length > 0)
        {
            List<Character> chars = await context.Characters
                .AsNoTracking()
                .Where(c => characterIds.Contains(c.Id))
                .ToListAsync();
            foreach (Character c in chars)
                map[c.Id] = new SourceLink("Character", c.Name, $"/characters/{c.Slug}");
        }

        if (pageIds.Length > 0)
        {
            List<WikiPage> pages = await context.WikiPages
                .AsNoTracking()
                .Where(p => pageIds.Contains(p.Id))
                .ToListAsync();
            foreach (WikiPage p in pages)
                map[p.Id] = new SourceLink("Page", p.Title, $"/logs/{p.Slug}");
        }

        if (eventIds.Length > 0)
        {
            List<TimelineEvent> events = await context.TimelineEvents
                .AsNoTracking()
                .Where(e => eventIds.Contains(e.Id))
                .ToListAsync();
            foreach (TimelineEvent e in events)
                map[e.Id] = new SourceLink("Event", e.Title, $"/timeline/{e.Id}");
        }

        return map;
    }
}
