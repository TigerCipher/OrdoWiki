namespace OrdoWiki.Web.Components.Pages.Timeline;

using Data.Calendars;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class TimelineList
{
    private List<TimelineEventDto> _allEvents = [];
    private List<YearGroup> _groups = [];
    private IReadOnlyList<MandoEraInfo> _eraInfos = [];
    private bool _loading = true;
    private bool _descending = true;

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private IMandoCalendarService Calendar { get; set; } = null!;

    [Inject]
    private IMarkdownService Markdown { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<List<TimelineEventDto>> response = await TimelineService.GetEventsAsync();
        _loading = false;

        if (!response.Success)
        {
            Snackbar.Add($"Failed to load timeline: {response.Error}", Severity.Error);
            return;
        }

        _allEvents = response.Value;
        IReadOnlyList<MandoEraDto> eras = await Calendar.GetErasAsync();
        _eraInfos = eras.Select(e => e.ToInfo()).ToList();

        Regroup();
    }

    private Task ToggleSortAsync()
    {
        _descending = !_descending;
        Regroup();
        return Task.CompletedTask;
    }

    private void Regroup()
    {
        IEnumerable<IGrouping<int, TimelineEventDto>> grouped = _allEvents.GroupBy(e => e.MandoYear);

        IEnumerable<IGrouping<int, TimelineEventDto>> orderedGroups = _descending
            ? grouped.OrderByDescending(g => g.Key)
            : grouped.OrderBy(g => g.Key);

        _groups = orderedGroups
            .Select(g => new YearGroup(
                g.Key,
                FormatYear(_eraInfos, g.Key),
                (_descending
                    ? g.OrderByDescending(e => e.EpochDayNumber)
                    : g.OrderBy(e => e.EpochDayNumber)).ToList()))
            .ToList();
    }

    private static string FormatYear(IReadOnlyList<MandoEraInfo> eras, int absoluteYear)
    {
        MandoEraInfo? era = MandoEraResolver.Resolve(eras, absoluteYear);
        return era is null
            ? absoluteYear.ToString()
            : $"{MandoEraResolver.DisplayYear(era.Value, absoluteYear)} {era.Value.ShortCode}";
    }

    private sealed record YearGroup(int AbsoluteYear, string YearLabel, List<TimelineEventDto> Events);
}
