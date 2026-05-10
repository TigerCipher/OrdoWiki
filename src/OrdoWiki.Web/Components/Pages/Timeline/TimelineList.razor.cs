namespace OrdoWiki.Web.Components.Pages.Timeline;

using Data.Calendars;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class TimelineList
{
    private const int PageSize = 25;

    private List<YearGroup> _groups = [];
    private IReadOnlyList<MandoEraDto> _eras = [];
    private IReadOnlyList<MandoEraInfo> _eraInfos = [];
    private IReadOnlyList<TagDto> _allTags = [];
    private int _totalCount;
    private int _totalPages;

    private bool _loading = true;
    private bool _descending = true;
    private Guid? _eraFilter;
    private TagDto? _selectedTag;
    private int? _minYear;
    private int? _maxYear;
    private int _page = 1;

    [SupplyParameterFromQuery(Name = "tag")]
    public string? TagSlug { get; set; }

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private IMandoCalendarService Calendar { get; set; } = null!;

    [Inject]
    private ITagService TagService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        _eras = await Calendar.GetErasAsync();
        _eraInfos = _eras.Select(e => e.ToInfo()).ToList();
        _allTags = await TagService.GetAllAsync();
        _selectedTag = string.IsNullOrEmpty(TagSlug)
            ? null
            : _allTags.FirstOrDefault(t => string.Equals(t.Slug, TagSlug, StringComparison.OrdinalIgnoreCase));

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;

        ApiResponse<PagedResult<TimelineEventDto>> response = await TimelineService.GetEventsAsync(new TimelineEventFilter
        {
            EraId = _eraFilter,
            MinDisplayYear = _minYear,
            MaxDisplayYear = _maxYear,
            TagId = _selectedTag?.Id,
            Descending = _descending,
            Page = _page,
            PageSize = PageSize,
        });

        _loading = false;

        if (!response.Success)
        {
            Snackbar.Add($"Failed to load timeline: {response.Error}", Severity.Error);
            return;
        }

        PagedResult<TimelineEventDto> page = response;
        _totalCount = page.TotalCount;
        _totalPages = page.TotalPages;
        Regroup(page.Items);
    }

    private async Task ToggleSortAsync()
    {
        _descending = !_descending;
        _page = 1;
        await LoadAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        _page = 1;
        await LoadAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _eraFilter = null;
        _minYear = null;
        _maxYear = null;
        _selectedTag = null;
        _page = 1;
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("tag", (string?)null), replace: true);
        await LoadAsync();
    }

    private async Task OnTagFilterChangedAsync(TagDto? tag)
    {
        _selectedTag = tag;
        _page = 1;
        string url = tag is null
            ? Navigation.GetUriWithQueryParameter("tag", (string?)null)
            : Navigation.GetUriWithQueryParameter("tag", tag.Slug);
        Navigation.NavigateTo(url, replace: true);
        await LoadAsync();
    }

    private async Task PageChangedAsync(int newPage)
    {
        _page = newPage;
        await LoadAsync();
    }

    private void Regroup(IReadOnlyList<TimelineEventDto> events)
    {
        IEnumerable<IGrouping<int, TimelineEventDto>> grouped = events.GroupBy(e => e.MandoYear);

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

    private string YearLabel(string suffix) =>
        _eraFilter is not null && _eras.FirstOrDefault(e => e.Id == _eraFilter) is { } selected
            ? $"{suffix} ({selected.ShortCode})"
            : suffix;

    private sealed record YearGroup(int AbsoluteYear, string YearLabel, List<TimelineEventDto> Events);
}
