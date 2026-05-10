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
    AuthenticationStateProvider authState,
    IAuthorizationService authorization) : ITimelineService
{
    public async Task<ApiResponse<List<TimelineEventDto>>> GetEventsAsync()
    {
        List<TimelineEvent> events = await context.TimelineEvents
            .AsNoTracking()
            .Include(e => e.CreatedBy)
            .OrderBy(e => e.EpochDayNumber)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(events.Select(e => e.CreatedById));

        List<TimelineEventDto> dtos = new(events.Count);
        foreach (TimelineEvent ev in events)
        {
            string display = ev.DisplayOverride ?? await calendar.FormatAsync(
                new MandoDate(ev.MandoYear, ev.MandoMonth, ev.MandoDay));
            dtos.Add(MapToDto(ev, roles.GetValueOrDefault(ev.CreatedById), display));
        }

        return Ok(dtos);
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

        return Ok(MapToDto(ev, roles.GetValueOrDefault(ev.CreatedById), display));
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
        ev.MarkdownBody = string.IsNullOrWhiteSpace(request.MarkdownBody) ? null : request.MarkdownBody;
        ev.MandoYear = request.MandoYear;
        ev.MandoMonth = request.MandoMonth;
        ev.MandoDay = request.MandoDay;
        ev.EpochDayNumber = MandoCalendar.ToEpochDay(new MandoDate(request.MandoYear, request.MandoMonth, request.MandoDay));
        ev.DisplayOverride = string.IsNullOrWhiteSpace(request.DisplayOverride) ? null : request.DisplayOverride!.Trim();
        ev.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return await GetEventByIdAsync(ev.Id);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
    {
        ApiResponse<bool> auth = await EnsureCanEditAsync<bool>();
        if (!auth.Success) return auth;

        TimelineEvent? ev = await context.TimelineEvents.SingleOrDefaultAsync(e => e.Id == id);
        if (ev is null) return NotFound<bool>();

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
