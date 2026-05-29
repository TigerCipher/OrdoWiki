namespace OrdoWiki.Web.Services;

using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Models.Requests;

public class RelatedItemsService(ApplicationDbContext context) : IRelatedItemsService
{
    public async Task<RelatedItemsDto> GetForAsync(RelatedItemKind kind, Guid id)
    {
        // A row connects this entity to another regardless of which side we are.
        // Pull all rows that mention us, then map "the other side" to a ref.
        List<RelatedItem> rows = await context.RelatedItems
            .AsNoTracking()
            .Where(r =>
                (r.SourceKind == kind && r.SourceId == id) ||
                (r.TargetKind == kind && r.TargetId == id))
            .ToListAsync();

        List<(RelatedItemKind Kind, Guid Id)> others = rows
            .Select(r => r.SourceKind == kind && r.SourceId == id
                ? (r.TargetKind, r.TargetId)
                : (r.SourceKind, r.SourceId))
            .Distinct()
            .ToList();

        RelatedItemsDto result = new();
        if (others.Count == 0) return result;

        HashSet<Guid> characterIds = others.Where(o => o.Kind == RelatedItemKind.Character).Select(o => o.Id).ToHashSet();
        HashSet<Guid> logIds = others.Where(o => o.Kind == RelatedItemKind.Log).Select(o => o.Id).ToHashSet();
        HashSet<Guid> eventIds = others.Where(o => o.Kind == RelatedItemKind.TimelineEvent).Select(o => o.Id).ToHashSet();

        if (characterIds.Count > 0)
        {
            result.Characters = await context.Characters
                .AsNoTracking()
                .Where(c => characterIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new RelatedItemRef
                {
                    Id = c.Id,
                    Kind = RelatedItemKind.Character,
                    Label = c.Name,
                    Href = "/characters/" + c.Slug,
                })
                .ToListAsync();
        }

        if (logIds.Count > 0)
        {
            result.Logs = await context.WikiPages
                .AsNoTracking()
                .Where(p => logIds.Contains(p.Id))
                .OrderBy(p => p.Title)
                .Select(p => new RelatedItemRef
                {
                    Id = p.Id,
                    Kind = RelatedItemKind.Log,
                    Label = p.Title,
                    Href = "/logs/" + p.Slug,
                })
                .ToListAsync();
        }

        if (eventIds.Count > 0)
        {
            // Timeline events have no slug — viewer routes by Id.
            result.TimelineEvents = await context.TimelineEvents
                .AsNoTracking()
                .Where(e => eventIds.Contains(e.Id))
                .OrderBy(e => e.EpochDayNumber)
                .Select(e => new RelatedItemRef
                {
                    Id = e.Id,
                    Kind = RelatedItemKind.TimelineEvent,
                    Label = e.Title,
                    Href = "/timeline/" + e.Id,
                })
                .ToListAsync();
        }

        return result;
    }

    public async Task<ApiResponse<RelatedItemsDto>> SetForAsync(RelatedItemKind kind, Guid id, SetRelatedItemsRequest request)
    {
        // Build desired (kind, id) set, dedup, drop self-relations.
        HashSet<(RelatedItemKind Kind, Guid Id)> desired = new();
        foreach (Guid cid in request.CharacterIds) desired.Add((RelatedItemKind.Character, cid));
        foreach (Guid lid in request.LogIds) desired.Add((RelatedItemKind.Log, lid));
        foreach (Guid eid in request.TimelineEventIds) desired.Add((RelatedItemKind.TimelineEvent, eid));
        desired.RemoveWhere(o => o.Kind == kind && o.Id == id);

        List<RelatedItem> existing = await context.RelatedItems
            .Where(r =>
                (r.SourceKind == kind && r.SourceId == id) ||
                (r.TargetKind == kind && r.TargetId == id))
            .ToListAsync();

        // Index existing rows by "the other endpoint" for diffing against desired.
        Dictionary<(RelatedItemKind, Guid), RelatedItem> existingByOther = existing.ToDictionary(
            r => r.SourceKind == kind && r.SourceId == id
                ? (r.TargetKind, r.TargetId)
                : (r.SourceKind, r.SourceId));

        // Remove rows whose other endpoint is no longer desired.
        foreach ((var other, RelatedItem row) in existingByOther)
        {
            if (!desired.Contains(other)) context.RelatedItems.Remove(row);
        }

        // Add rows for newly desired endpoints, in canonical order to avoid dupes.
        DateTime now = DateTime.UtcNow;
        foreach (var other in desired)
        {
            if (existingByOther.ContainsKey(other)) continue;

            (RelatedItemKind sKind, Guid sId, RelatedItemKind tKind, Guid tId) = Canonicalize(kind, id, other.Kind, other.Id);
            context.RelatedItems.Add(new RelatedItem
            {
                Id = Guid.NewGuid(),
                SourceKind = sKind,
                SourceId = sId,
                TargetKind = tKind,
                TargetId = tId,
                CreatedAt = now,
            });
        }

        await context.SaveChangesAsync();
        return Ok(await GetForAsync(kind, id));
    }

    public async Task<IReadOnlyList<RelatedItemRef>> SearchAsync(RelatedItemKind kind, string? query, int limit = 50)
    {
        string trimmed = (query ?? string.Empty).Trim();
        string like = $"%{trimmed}%";

        return kind switch
        {
            RelatedItemKind.Character => await context.Characters
                .AsNoTracking()
                .Where(c => trimmed.Length == 0 || EF.Functions.ILike(c.Name, like))
                .OrderBy(c => c.Name)
                .Take(limit)
                .Select(c => new RelatedItemRef
                {
                    Id = c.Id,
                    Kind = RelatedItemKind.Character,
                    Label = c.Name,
                    Href = "/characters/" + c.Slug,
                })
                .ToListAsync(),
            RelatedItemKind.Log => await context.WikiPages
                .AsNoTracking()
                .Where(p => trimmed.Length == 0 || EF.Functions.ILike(p.Title, like))
                .OrderBy(p => p.Title)
                .Take(limit)
                .Select(p => new RelatedItemRef
                {
                    Id = p.Id,
                    Kind = RelatedItemKind.Log,
                    Label = p.Title,
                    Href = "/logs/" + p.Slug,
                })
                .ToListAsync(),
            RelatedItemKind.TimelineEvent => await context.TimelineEvents
                .AsNoTracking()
                .Where(e => trimmed.Length == 0 || EF.Functions.ILike(e.Title, like))
                .OrderBy(e => e.EpochDayNumber)
                .Take(limit)
                .Select(e => new RelatedItemRef
                {
                    Id = e.Id,
                    Kind = RelatedItemKind.TimelineEvent,
                    Label = e.Title,
                    Href = "/timeline/" + e.Id,
                })
                .ToListAsync(),
            _ => [],
        };
    }

    public async Task DeleteAllForAsync(RelatedItemKind kind, Guid id)
    {
        await context.RelatedItems
            .Where(r =>
                (r.SourceKind == kind && r.SourceId == id) ||
                (r.TargetKind == kind && r.TargetId == id))
            .ExecuteDeleteAsync();
    }

    // Stable ordering by (Kind, Id) makes A<->B always canonicalize to the same
    // row regardless of which side called Set.
    private static (RelatedItemKind, Guid, RelatedItemKind, Guid) Canonicalize(
        RelatedItemKind aKind, Guid aId, RelatedItemKind bKind, Guid bId)
    {
        int byKind = ((int)aKind).CompareTo((int)bKind);
        int cmp = byKind != 0 ? byKind : aId.CompareTo(bId);
        return cmp <= 0 ? (aKind, aId, bKind, bId) : (bKind, bId, aKind, aId);
    }
}
