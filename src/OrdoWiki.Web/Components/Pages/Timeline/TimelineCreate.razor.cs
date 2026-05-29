namespace OrdoWiki.Web.Components.Pages.Timeline;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class TimelineCreate
{
    private string _title = string.Empty;
    private string _summary = string.Empty;
    private string _body = string.Empty;
    private string _displayOverride = string.Empty;
    private int _year;
    private int? _month;
    private int? _day;
    private bool _saving;
    private IReadOnlyList<string> _tagNames = [];
    private RelatedItemsDto _related = new();

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private void OnTagsChanged(IReadOnlyList<string> tags) => _tagNames = tags;

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
            Summary = _summary,
            MarkdownBody = _body,
            MandoYear = _year,
            MandoMonth = _month,
            MandoDay = _day,
            DisplayOverride = _displayOverride,
            Tags = _tagNames,
        });

        if (!response.Success)
        {
            _saving = false;
            Snackbar.Add($"Failed to create event: {response.Error}", Severity.Error);
            return;
        }

        if (!_related.IsEmpty)
        {
            await RelatedItemsService.SetForAsync(
                RelatedItemKind.TimelineEvent,
                response.Value.Id,
                new SetRelatedItemsRequest
                {
                    CharacterIds = _related.Characters.Select(r => r.Id).ToList(),
                    LogIds = _related.Logs.Select(r => r.Id).ToList(),
                    TimelineEventIds = _related.TimelineEvents.Select(r => r.Id).ToList(),
                });
        }

        _saving = false;
        Snackbar.Add("Event created.", Severity.Success);
        Navigation.NavigateTo("/timeline");
    }
}
