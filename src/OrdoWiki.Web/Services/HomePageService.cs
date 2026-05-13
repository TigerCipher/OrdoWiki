namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Calendars;
using Data.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Models.Requests;

public class HomePageService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IUserService userService,
    IPageService pageService,
    IMandoCalendarService calendar,
    IMarkdownService markdown,
    AuthenticationStateProvider authState) : IHomePageService
{
    private const int RecentEventCount = 3;

    public async Task<HomePageDto> GetAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        HomePage row = await context.HomePages
            .AsNoTracking()
            .SingleAsync(h => h.Id == HomePage.SingletonId);

        HomePageDto dto = new()
        {
            BioMarkdown = row.BioMarkdown,
            BioHtml = markdown.Render(row.BioMarkdown),
            FeaturedLogId = row.FeaturedLogId,
        };

        if (row.FeaturedLogId is { } featuredId)
        {
            ApiResponse<WikiPageDto> featured = await pageService.GetPageByIdAsync(featuredId);
            if (featured.Success) dto.FeaturedLog = featured.Value;
        }

        List<TimelineEvent> recent = await context.TimelineEvents
            .AsNoTracking()
            .OrderByDescending(e => e.EpochDayNumber)
            .ThenByDescending(e => e.Id)
            .Take(RecentEventCount)
            .ToListAsync();

        foreach (TimelineEvent ev in recent)
        {
            string display = ev.DisplayOverride ?? await calendar.FormatAsync(
                new MandoDate(ev.MandoYear, ev.MandoMonth, ev.MandoDay));

            dto.RecentEvents.Add(new TimelineEventDto
            {
                Id = ev.Id,
                Title = ev.Title,
                Summary = ev.Summary,
                MandoYear = ev.MandoYear,
                MandoMonth = ev.MandoMonth,
                MandoDay = ev.MandoDay,
                EpochDayNumber = ev.EpochDayNumber,
                DisplayOverride = ev.DisplayOverride,
                DisplayDate = display,
                CreatedById = ev.CreatedById,
                CreatedAt = ev.CreatedAt,
                UpdatedAt = ev.UpdatedAt,
            });
        }

        return dto;
    }

    public async Task<ApiResponse<HomePageDto>> UpdateBioAsync(UpdateBioRequest request)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync();
        if (!auth.Success) return Forbidden<HomePageDto>(auth.Error);

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        HomePage row = await context.HomePages.SingleAsync(h => h.Id == HomePage.SingletonId);

        ApiResponse<UserDto> me = await userService.GetCurrentUserAsync();

        row.BioMarkdown = request.BioMarkdown ?? string.Empty;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedById = me.Success ? me.Value.Id : null;

        await context.SaveChangesAsync();
        return Ok(await GetAsync());
    }

    public async Task<ApiResponse<HomePageDto>> SetFeaturedLogAsync(SetFeaturedLogRequest request)
    {
        ApiResponse<bool> auth = await EnsureAdminAsync();
        if (!auth.Success) return Forbidden<HomePageDto>(auth.Error);

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        if (request.FeaturedLogId is { } id)
        {
            bool exists = await context.WikiPages.AnyAsync(p => p.Id == id);
            if (!exists) return BadRequest<HomePageDto>("Selected log does not exist.");
        }

        HomePage row = await context.HomePages.SingleAsync(h => h.Id == HomePage.SingletonId);
        ApiResponse<UserDto> me = await userService.GetCurrentUserAsync();

        row.FeaturedLogId = request.FeaturedLogId;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedById = me.Success ? me.Value.Id : null;

        await context.SaveChangesAsync();
        return Ok(await GetAsync());
    }

    private async Task<ApiResponse<bool>> EnsureAdminAsync()
    {
        AuthenticationState state = await authState.GetAuthenticationStateAsync();
        return state.User.IsInRole(Roles.Admin)
            ? Ok(true)
            : Forbidden<bool>("Only administrators can edit the home page.");
    }
}
