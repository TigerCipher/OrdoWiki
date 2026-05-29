namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Calendars;
using Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Models.Requests;
using System.Security.Claims;

public class TimelineService(
    ApplicationDbContext context,
    IUserService userService,
    IMandoCalendarService calendar,
    ITagService tagService,
    IRelatedItemsService relatedItemsService,
    AuthenticationStateProvider authState,
    IAuthorizationService authorization) : ITimelineService
{
    public async Task<ApiResponse<PagedResult<TimelineEventDto>>> GetEventsAsync(TimelineEventFilter filter)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        IReadOnlyList<MandoEraDto> eras = await calendar.GetErasAsync();
        (int? minYear, int? maxYear) = ResolveYearBounds(filter, eras);

        IQueryable<TimelineEvent> query = context.TimelineEvents.AsNoTracking();
        if (minYear.HasValue) query = query.Where(e => e.MandoYear >= minYear.Value);
        if (maxYear.HasValue) query = query.Where(e => e.MandoYear <= maxYear.Value);
        if (filter.TagId is { } tagId)
            query = query.Where(e => context.TimelineEventTags.Any(j => j.TimelineEventId == e.Id && j.TagId == tagId));

        int total = await query.CountAsync();

        query = filter.Descending
            ? query.OrderByDescending(e => e.EpochDayNumber).ThenByDescending(e => e.Id)
            : query.OrderBy(e => e.EpochDayNumber).ThenBy(e => e.Id);

        List<TimelineEvent> events = await query
            .Include(e => e.CreatedBy)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(events.Select(e => e.CreatedById));

        // Batch tag lookup to avoid N+1 across the page.
        List<Guid> eventIds = events.Select(e => e.Id).ToList();
        Dictionary<Guid, List<TagDto>> tagsByEvent = await context.TimelineEventTags
            .AsNoTracking()
            .Where(j => eventIds.Contains(j.TimelineEventId))
            .Select(j => new { j.TimelineEventId, Tag = new TagDto { Id = j.Tag.Id, Slug = j.Tag.Slug, Name = j.Tag.Name } })
            .ToListAsync()
            .ContinueWith(t => t.Result
                .GroupBy(x => x.TimelineEventId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Tag).OrderBy(t => t.Name).ToList()));

        List<TimelineEventDto> dtos = new(events.Count);
        foreach (TimelineEvent ev in events)
        {
            string display = ev.DisplayOverride ?? await calendar.FormatAsync(
                new MandoDate(ev.MandoYear, ev.MandoMonth, ev.MandoDay));
            TimelineEventDto dto = MapToDto(ev, roles.GetValueOrDefault(ev.CreatedById), display);
            dto.Tags = tagsByEvent.GetValueOrDefault(ev.Id, []);
            dtos.Add(dto);
        }

        return Ok(new PagedResult<TimelineEventDto>(dtos, total, page, pageSize));
    }

    /// <summary>
    /// Translate the filter's optional era + year bounds into absolute-year bounds against
    /// <see cref="TimelineEvent.MandoYear"/>. Year fields are display-years inside the era when
    /// an era is selected, or absolute signed years otherwise.
    /// </summary>
    private static (int? min, int? max) ResolveYearBounds(TimelineEventFilter filter, IReadOnlyList<MandoEraDto> eras)
    {
        if (filter.EraId is null)
        {
            int? min = filter.MinDisplayYear;
            int? max = filter.MaxDisplayYear;
            if (min.HasValue && max.HasValue && min > max) (min, max) = (max, min);
            return (min, max);
        }

        MandoEraDto? era = eras.FirstOrDefault(e => e.Id == filter.EraId);
        if (era is null) return (null, null);
        MandoEraInfo info = era.ToInfo();

        (int? eraMin, int? eraMax) = GetEraBounds(info, eras);

        if (filter.MinDisplayYear is null && filter.MaxDisplayYear is null)
            return (eraMin, eraMax);

        // Display years convert to absolute via the era's direction. For Backward, larger
        // display year = smaller absolute year, so order may flip after conversion.
        int? userA = filter.MinDisplayYear.HasValue
            ? MandoEraResolver.ToAbsoluteYear(info, filter.MinDisplayYear.Value)
            : (int?)null;
        int? userB = filter.MaxDisplayYear.HasValue
            ? MandoEraResolver.ToAbsoluteYear(info, filter.MaxDisplayYear.Value)
            : (int?)null;

        int? userMin = (userA, userB) switch
        {
            (null, null) => null,
            ({ } a, null) => a,
            (null, { } b) => b,
            ({ } a, { } b) => Math.Min(a, b),
        };
        int? userMax = (userA, userB) switch
        {
            (null, null) => null,
            ({ } a, null) => a,
            (null, { } b) => b,
            ({ } a, { } b) => Math.Max(a, b),
        };

        int? finalMin = (eraMin, userMin) switch
        {
            (null, _) => userMin,
            (_, null) => eraMin,
            ({ } a, { } b) => Math.Max(a, b),
        };
        int? finalMax = (eraMax, userMax) switch
        {
            (null, _) => userMax,
            (_, null) => eraMax,
            ({ } a, { } b) => Math.Min(a, b),
        };

        return (finalMin, finalMax);
    }

    private static (int? min, int? max) GetEraBounds(MandoEraInfo era, IReadOnlyList<MandoEraDto> allEras)
    {
        if (era.Direction == EraDirection.Forward)
        {
            int? nextAnchor = allEras
                .Where(e => e.Direction == EraDirection.Forward && e.AnchorYear > era.AnchorYear)
                .Select(e => (int?)e.AnchorYear)
                .Min();
            return (era.AnchorYear, nextAnchor.HasValue ? nextAnchor.Value - 1 : (int?)null);
        }

        int? prevAnchor = allEras
            .Where(e => e.Direction == EraDirection.Backward && e.AnchorYear < era.AnchorYear)
            .Select(e => (int?)e.AnchorYear)
            .Max();
        return (prevAnchor, era.AnchorYear - 1);
    }

    public async Task<ApiResponse<TimelineEventDto>> GetEventByIdAsync(Guid id)
    {
        TimelineEvent? ev = await context.TimelineEvents
            .AsNoTracking()
            .Include(e => e.CreatedBy)
            .SingleOrDefaultAsync(e => e.Id == id);

        if (ev is null) return NotFound<TimelineEventDto>();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync([ev.CreatedById]);
        string display = ev.DisplayOverride ?? await calendar.FormatAsync(
            new MandoDate(ev.MandoYear, ev.MandoMonth, ev.MandoDay));

        TimelineEventDto dto = MapToDto(ev, roles.GetValueOrDefault(ev.CreatedById), display);
        dto.Tags = await tagService.GetTagsForAsync(TagTarget.TimelineEvent, ev.Id);
        return Ok(dto);
    }

    public async Task<ApiResponse<TimelineEventDto>> CreateAsync(CreateTimelineEventRequest request)
    {
        ApiResponse<bool> auth = await EnsureCanEditAsync<bool>();
        if (!auth.Success) return Forbidden<TimelineEventDto>(auth.Error);

        ApiResponse<bool> validation = ValidateDate(request.MandoMonth, request.MandoDay);
        if (!validation.Success) return BadRequest<TimelineEventDto>(validation.Error ?? "Invalid date.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest<TimelineEventDto>("Title is required.");

        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<TimelineEventDto>(userResponse.Error);

        DateTime now = DateTime.UtcNow;
        TimelineEvent ev = new()
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary!.Trim(),
            MarkdownBody = string.IsNullOrWhiteSpace(request.MarkdownBody) ? null : request.MarkdownBody,
            MandoYear = request.MandoYear,
            MandoMonth = request.MandoMonth,
            MandoDay = request.MandoDay,
            EpochDayNumber = MandoCalendar.ToEpochDay(new MandoDate(request.MandoYear, request.MandoMonth, request.MandoDay)),
            DisplayOverride = string.IsNullOrWhiteSpace(request.DisplayOverride) ? null : request.DisplayOverride!.Trim(),
            CreatedById = userResponse.Value.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.TimelineEvents.Add(ev);
        await context.SaveChangesAsync();

        if (request.Tags is not null)
            await tagService.SetTagsAsync(TagTarget.TimelineEvent, ev.Id, request.Tags);

        return await GetEventByIdAsync(ev.Id);
    }

    public async Task<ApiResponse<TimelineEventDto>> UpdateAsync(UpdateTimelineEventRequest request)
    {
        ApiResponse<bool> auth = await EnsureCanEditAsync<bool>();
        if (!auth.Success) return Forbidden<TimelineEventDto>(auth.Error);

        TimelineEvent? ev = await context.TimelineEvents.SingleOrDefaultAsync(e => e.Id == request.Id);
        if (ev is null) return NotFound<TimelineEventDto>();

        ApiResponse<bool> validation = ValidateDate(request.MandoMonth, request.MandoDay);
        if (!validation.Success) return BadRequest<TimelineEventDto>(validation.Error ?? "Invalid date.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest<TimelineEventDto>("Title is required.");

        ev.Title = request.Title.Trim();
        ev.Summary = string.IsNullOrWhiteSpace(request.Summary) ? null : request.Summary!.Trim();
        ev.MarkdownBody = string.IsNullOrWhiteSpace(request.MarkdownBody) ? null : request.MarkdownBody;
        ev.MandoYear = request.MandoYear;
        ev.MandoMonth = request.MandoMonth;
        ev.MandoDay = request.MandoDay;
        ev.EpochDayNumber = MandoCalendar.ToEpochDay(new MandoDate(request.MandoYear, request.MandoMonth, request.MandoDay));
        ev.DisplayOverride = string.IsNullOrWhiteSpace(request.DisplayOverride) ? null : request.DisplayOverride!.Trim();
        ev.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        if (request.Tags is not null)
            await tagService.SetTagsAsync(TagTarget.TimelineEvent, ev.Id, request.Tags);

        return await GetEventByIdAsync(ev.Id);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        ApiResponse<bool> auth = await EnsureCanEditAsync<bool>();
        if (!auth.Success) return auth;

        TimelineEvent? ev = await context.TimelineEvents.SingleOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound<bool>();

        await relatedItemsService.DeleteAllForAsync(Data.Entities.RelatedItemKind.TimelineEvent, id);

        context.TimelineEvents.Remove(ev);
        await context.SaveChangesAsync();
        return Ok(true);
    }

    private static ApiResponse<bool> ValidateDate(int? month, int? day)
    {
        if (month is < 1 or > MandoCalendar.MonthsPerYear)
            return BadRequest<bool>("Month must be between 1 and 12, or unspecified.");
        if (day is < 1 or > MandoCalendar.DaysPerMonth)
            return BadRequest<bool>("Day must be between 1 and 30, or unspecified.");
        if (day is not null && month is null)
            return BadRequest<bool>("A day requires a month.");
        return Ok(true);
    }

    private async Task<ApiResponse<T>> EnsureCanEditAsync<T>()
    {
        AuthenticationState state = await authState.GetAuthenticationStateAsync();
        ClaimsPrincipal user = state.User;
        AuthorizationResult result = await authorization.AuthorizeAsync(user, Policies.CanEdit);
        return result.Succeeded
            ? Ok(default(T)!)
            : Forbidden<T>("You don't have permission to edit timeline events.");
    }
}
