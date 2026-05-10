namespace OrdoWiki.Web.Services;

using Data;
using Data.Entities;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Models;

public class GalleryService(
    ApplicationDbContext context,
    IUserService userService) : IGalleryService
{
    public async Task<ApiResponse<PagedResult<GalleryItemDto>>> GetGalleryAsync(GalleryFilter filter)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        IQueryable<MediaAsset> q = context.MediaAssets
            .AsNoTracking()
            .Include(a => a.UploadedBy)
            .Where(a => a.SourceType != MediaSourceType.Avatar);

        if (filter.SourceType is { } src)
            q = q.Where(a => a.SourceType == src);

        if (!string.IsNullOrEmpty(filter.UploaderId))
            q = q.Where(a => a.UploadedById == filter.UploaderId);

        int total = await q.CountAsync();

        List<MediaAsset> items = await q
            .OrderByDescending(a => a.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(
            items.Select(a => a.UploadedById));

        Dictionary<Guid, SourceLink> sources = await ResolveSourcesAsync(items);

        List<GalleryItemDto> dtos = items.Select(a => new GalleryItemDto
        {
            Asset = MapToDto(a, roles.GetValueOrDefault(a.UploadedById)),
            Source = a.SourceId.HasValue
                ? sources.GetValueOrDefault(a.SourceId.Value)
                : null,
        }).ToList();

        return Ok(new PagedResult<GalleryItemDto>(dtos, total, page, pageSize));
    }

    public async Task<ApiResponse<List<UserDto>>> GetUploadersAsync()
    {
        List<ApplicationUser> users = await context.MediaAssets
            .AsNoTracking()
            .Where(a => a.SourceType != MediaSourceType.Avatar)
            .Select(a => a.UploadedBy!)
            .Distinct()
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(users.Select(u => u.Id));

        return Ok(users.Select(u => MapToDto(u, roles.GetValueOrDefault(u.Id))).ToList());
    }

    private async Task<Dictionary<Guid, SourceLink>> ResolveSourcesAsync(IReadOnlyList<MediaAsset> assets)
    {
        Guid[] characterIds = assets
            .Where(a => a.SourceType == MediaSourceType.Character && a.SourceId.HasValue)
            .Select(a => a.SourceId!.Value).Distinct().ToArray();

        Guid[] pageIds = assets
            .Where(a => a.SourceType == MediaSourceType.WikiPage && a.SourceId.HasValue)
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

        return map;
    }
}
