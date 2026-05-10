namespace OrdoWiki.Web.Components.Pages.Timeline;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class TimelineCreate
{
    private string _title = string.Empty;
    private string _body = string.Empty;
    private string _displayOverride = string.Empty;
    private int _year;
    private int? _month;
    private int? _day;
    private bool _saving;

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_title))
        {
            Snackbar.Add("Title is required.", Severity.Warning);
            return;
        }

        _saving = true;
        ApiResponse<TimelineEventDto> response = await TimelineService.CreateAsync(new CreateTimelineEventRequest
        {
            Title = _title,
            MarkdownBody = _body,
            MandoYear = _year,
            MandoMonth = _month,
            MandoDay = _day,
            DisplayOverride = _displayOverride,
        });
        _saving = false;

        if (!response.Success)
        {
            Snackbar.Add($"Failed to create event: {response.Error}", Severity.Error);
            return;
        }

        Snackbar.Add("Event created.", Severity.Success);
        Navigation.NavigateTo("/timeline");
    }
}
