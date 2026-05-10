namespace OrdoWiki.Web.Services;

using Data;
using Data.Entities;
using Helpers;
using Microsoft.EntityFrameworkCore;

public class TagService(ApplicationDbContext context, IUserService userService) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetAllAsync()
    {
        List<TagDto> tags = await context.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto { Id = t.Id, Slug = t.Slug, Name = t.Name })
            .ToListAsync();

        return tags;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllWithCountsAsync()
    {
        // One query per join table — simpler than UNION ALL and EF can compose each.
        var counts = await context.Tags
            .AsNoTracking()
            .Select(t => new
            {
                t.Id,
                t.Slug,
                t.Name,
                Pages = context.WikiPageTags.Count(j => j.TagId == t.Id),
                Characters = context.CharacterTags.Count(j => j.TagId == t.Id),
                Media = context.MediaAssetTags.Count(j => j.TagId == t.Id),
                Events = context.TimelineEventTags.Count(j => j.TagId == t.Id),
            })
            .OrderBy(t => t.Name)
            .ToListAsync();

        return counts
            .Select(t => new TagDto
            {
                Id = t.Id,
                Slug = t.Slug,
                Name = t.Name,
                UsageCount = t.Pages + t.Characters + t.Media + t.Events,
            })
            .ToList();
    }

    public async Task<TagDto?> GetBySlugAsync(string slug)
    {
        Tag? tag = await context.Tags.AsNoTracking().SingleOrDefaultAsync(t => t.Slug == slug);
        return tag is null ? null : new TagDto { Id = tag.Id, Slug = tag.Slug, Name = tag.Name };
    }

    public async Task<IReadOnlyList<TagDto>> SearchAsync(string query, int limit = 20)
    {
        IQueryable<Tag> q = context.Tags.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            string trimmed = query.Trim();
            q = q.Where(t => EF.Functions.ILike(t.Name, $"%{trimmed}%"));
        }

        return await q.OrderBy(t => t.Name)
            .Take(limit)
            .Select(t => new TagDto { Id = t.Id, Slug = t.Slug, Name = t.Name })
            .ToListAsync();
    }

    public async Task<ApiResponse<TagDto>> GetOrCreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest<TagDto>("Tag name is required.");

        string trimmed = name.Trim();
        string slug = trimmed.CreateSlug();
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest<TagDto>("Tag name produced an empty slug.");

        Tag? existing = await context.Tags.SingleOrDefaultAsync(t => t.Slug == slug);
        if (existing is not null)
            return Ok(new TagDto { Id = existing.Id, Slug = existing.Slug, Name = existing.Name });

        ApiResponse<UserDto> user = await userService.GetCurrentUserAsync();

        Tag tag = new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = trimmed,
            CreatedAt = DateTime.UtcNow,
            CreatedById = user.Success ? user.Value.Id : null,
        };

        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        return Ok(new TagDto { Id = tag.Id, Slug = tag.Slug, Name = tag.Name });
    }

    public async Task<IReadOnlyList<TagDto>> GetTagsForAsync(TagTarget target, Guid entityId)
    {
        IQueryable<Tag> q = target switch
        {
            TagTarget.WikiPage => context.WikiPageTags.Where(j => j.PageId == entityId).Select(j => j.Tag),
            TagTarget.Character => context.CharacterTags.Where(j => j.CharacterId == entityId).Select(j => j.Tag),
            TagTarget.MediaAsset => context.MediaAssetTags.Where(j => j.MediaAssetId == entityId).Select(j => j.Tag),
            TagTarget.TimelineEvent => context.TimelineEventTags.Where(j => j.TimelineEventId == entityId).Select(j => j.Tag),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };

        return await q
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto { Id = t.Id, Slug = t.Slug, Name = t.Name })
            .ToListAsync();
    }

    public async Task<ApiResponse<bool>> SetTagsAsync(TagTarget target, Guid entityId, IEnumerable<string> tagNames)
    {
        // Normalize input to (slug, displayName) pairs, deduped by slug.
        Dictionary<string, string> requested = tagNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Select(n => (Slug: n.CreateSlug(), Name: n))
            .Where(x => !string.IsNullOrWhiteSpace(x.Slug))
            .GroupBy(x => x.Slug)
            .ToDictionary(g => g.Key, g => g.First().Name);

        // Existing Tag rows for the requested slugs.
        List<Tag> existing = await context.Tags
            .Where(t => requested.Keys.Contains(t.Slug))
            .ToListAsync();

        Dictionary<string, Tag> bySlug = existing.ToDictionary(t => t.Slug);

        // Create any tags that don't exist yet.
        DateTime now = DateTime.UtcNow;
        ApiResponse<UserDto> user = await userService.GetCurrentUserAsync();
        string? createdById = user.Success ? user.Value.Id : null;

        foreach ((string slug, string name) in requested)
        {
            if (bySlug.ContainsKey(slug)) continue;
            Tag t = new()
            {
                Id = Guid.NewGuid(),
                Slug = slug,
                Name = name,
                CreatedAt = now,
                CreatedById = createdById,
            };
            context.Tags.Add(t);
            bySlug[slug] = t;
        }

        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync();

        HashSet<Guid> desiredTagIds = bySlug.Values.Select(t => t.Id).ToHashSet();
        HashSet<Guid> currentTagIds = await GetCurrentTagIdsAsync(target, entityId);

        IEnumerable<Guid> toAdd = desiredTagIds.Except(currentTagIds);
        IEnumerable<Guid> toRemove = currentTagIds.Except(desiredTagIds);

        AddJoinRows(target, entityId, toAdd);
        await RemoveJoinRowsAsync(target, entityId, toRemove);

        await context.SaveChangesAsync();
        return Ok(true);
    }

    private async Task<HashSet<Guid>> GetCurrentTagIdsAsync(TagTarget target, Guid entityId) =>
        target switch
        {
            TagTarget.WikiPage => (await context.WikiPageTags
                .Where(j => j.PageId == entityId).Select(j => j.TagId).ToListAsync()).ToHashSet(),
            TagTarget.Character => (await context.CharacterTags
                .Where(j => j.CharacterId == entityId).Select(j => j.TagId).ToListAsync()).ToHashSet(),
            TagTarget.MediaAsset => (await context.MediaAssetTags
                .Where(j => j.MediaAssetId == entityId).Select(j => j.TagId).ToListAsync()).ToHashSet(),
            TagTarget.TimelineEvent => (await context.TimelineEventTags
                .Where(j => j.TimelineEventId == entityId).Select(j => j.TagId).ToListAsync()).ToHashSet(),
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };

    private void AddJoinRows(TagTarget target, Guid entityId, IEnumerable<Guid> tagIds)
    {
        switch (target)
        {
            case TagTarget.WikiPage:
                context.WikiPageTags.AddRange(tagIds.Select(id => new WikiPageTag { PageId = entityId, TagId = id }));
                break;
            case TagTarget.Character:
                context.CharacterTags.AddRange(tagIds.Select(id => new CharacterTag { CharacterId = entityId, TagId = id }));
                break;
            case TagTarget.MediaAsset:
                context.MediaAssetTags.AddRange(tagIds.Select(id => new MediaAssetTag { MediaAssetId = entityId, TagId = id }));
                break;
            case TagTarget.TimelineEvent:
                context.TimelineEventTags.AddRange(tagIds.Select(id => new TimelineEventTag { TimelineEventId = entityId, TagId = id }));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
    }

    private async Task RemoveJoinRowsAsync(TagTarget target, Guid entityId, IEnumerable<Guid> tagIds)
    {
        HashSet<Guid> ids = tagIds.ToHashSet();
        if (ids.Count == 0) return;

        switch (target)
        {
            case TagTarget.WikiPage:
                List<WikiPageTag> wp = await context.WikiPageTags
                    .Where(j => j.PageId == entityId && ids.Contains(j.TagId)).ToListAsync();
                context.WikiPageTags.RemoveRange(wp);
                break;
            case TagTarget.Character:
                List<CharacterTag> ct = await context.CharacterTags
                    .Where(j => j.CharacterId == entityId && ids.Contains(j.TagId)).ToListAsync();
                context.CharacterTags.RemoveRange(ct);
                break;
            case TagTarget.MediaAsset:
                List<MediaAssetTag> mt = await context.MediaAssetTags
                    .Where(j => j.MediaAssetId == entityId && ids.Contains(j.TagId)).ToListAsync();
                context.MediaAssetTags.RemoveRange(mt);
                break;
            case TagTarget.TimelineEvent:
                List<TimelineEventTag> et = await context.TimelineEventTags
                    .Where(j => j.TimelineEventId == entityId && ids.Contains(j.TagId)).ToListAsync();
                context.TimelineEventTags.RemoveRange(et);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }
    }
}
