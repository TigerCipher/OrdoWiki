namespace OrdoWiki.Web.Components.Shared;

using Data.Calendars;
using Microsoft.AspNetCore.Components;

public partial class MandoDateInput
{
    private List<MandoEraDto> _eras = [];
    private List<MandoMonthDto> _months = [];
    private bool _loading = true;
    private Guid? _selectedEraId;
    private int _displayYear;
    private string _preview = string.Empty;

    /// <summary>Signed absolute year. Two-way bound.</summary>
    [Parameter]
    public int Year { get; set; }

    [Parameter]
    public EventCallback<int> YearChanged { get; set; }

    [Parameter]
    public int? Month { get; set; }

    [Parameter]
    public EventCallback<int?> MonthChanged { get; set; }

    [Parameter]
    public int? Day { get; set; }

    [Parameter]
    public EventCallback<int?> DayChanged { get; set; }

    [Inject]
    private IMandoCalendarService Calendar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        _eras = (await Calendar.GetErasAsync()).ToList();
        _months = (await Calendar.GetMonthsAsync()).ToList();
        _loading = false;

        InitFromAbsoluteYear();
        await UpdatePreviewAsync();
    }

    private void InitFromAbsoluteYear()
    {
        IReadOnlyList<MandoEraInfo> eraInfos = _eras.Select(e => e.ToInfo()).ToList();
        MandoEraInfo? era = MandoEraResolver.Resolve(eraInfos, Year);

        if (era is not null)
        {
            MandoEraDto match = _eras.First(e =>
                e.ShortCode == era.Value.ShortCode && e.AnchorYear == era.Value.AnchorYear);
            _selectedEraId = match.Id;
            _displayYear = MandoEraResolver.DisplayYear(era.Value, Year);
        }
        else if (_eras.Count > 0)
        {
            // No era covers this absolute year (e.g., before any defined era).
            // Fall back to the first era's display formula and leave the user to fix it.
            _selectedEraId = _eras[0].Id;
            _displayYear = MandoEraResolver.DisplayYear(_eras[0].ToInfo(), Year);
        }
    }

    private async Task OnEraChanged(Guid? eraId)
    {
        _selectedEraId = eraId;
        await RecomputeYearAsync();
    }

    private async Task OnDisplayYearChanged(int year)
    {
        _displayYear = year;
        await RecomputeYearAsync();
    }

    private async Task OnMonthChanged(int? month)
    {
        Month = month;
        if (month is null) Day = null;
        await MonthChanged.InvokeAsync(month);
        if (month is null) await DayChanged.InvokeAsync(null);
        await UpdatePreviewAsync();
    }

    private async Task OnDayChanged(int? day)
    {
        Day = day;
        await DayChanged.InvokeAsync(day);
        await UpdatePreviewAsync();
    }

    private async Task RecomputeYearAsync()
    {
        if (_selectedEraId is null) return;

        MandoEraDto? era = _eras.FirstOrDefault(e => e.Id == _selectedEraId);
        if (era is null) return;

        Year = MandoEraResolver.ToAbsoluteYear(era.ToInfo(), _displayYear);
        await YearChanged.InvokeAsync(Year);
        await UpdatePreviewAsync();
    }

    private async Task UpdatePreviewAsync()
    {
        _preview = await Calendar.FormatAsync(new MandoDate(Year, Month, Day));
    }
}
