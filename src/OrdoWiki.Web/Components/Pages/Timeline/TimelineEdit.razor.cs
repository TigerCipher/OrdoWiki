namespace OrdoWiki.Web.Components.Pages.Timeline;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class TimelineEdit
{
    private string _title = string.Empty;
    private string _summary = string.Empty;
    private string _body = string.Empty;
    private string _displayOverride = string.Empty;
    private int _year;
    private int? _month;
    private int? _day;
    private bool _loading = true;
    private bool _saving;
    private IReadOnlyList<string> _tagNames = [];
    private RelatedItemsDto _related = new();

    [Parameter, EditorRequired]
    public required Guid Id { get; set; }

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private IDialogService Dialog { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<TimelineEventDto> response = await TimelineService.GetEventByIdAsync(Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to load event: {response.Error}", Severity.Error);
            Navigation.NavigateTo("/timeline");
            return;
        }

        TimelineEventDto ev = response;
        _title = ev.Title;
        _summary = ev.Summary ?? string.Empty;
        _body = ev.MarkdownBody ?? string.Empty;
        _displayOverride = ev.DisplayOverride ?? string.Empty;
        _year = ev.MandoYear;
        _month = ev.MandoMonth;
        _day = ev.MandoDay;
        _tagNames = ev.Tags.Select(t => t.Name).ToList();
        _related = await RelatedItemsService.GetForAsync(RelatedItemKind.TimelineEvent, Id);
        _loading = false;
    }

    private void OnTagsChanged(IReadOnlyList<string> tags) => _tagNames = tags;

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_title))
        {
            Snackbar.Add("Title is required.", Severity.Warning);
            return;
        }

        _saving = true;
        ApiResponse<TimelineEventDto> response = await TimelineService.UpdateAsync(new UpdateTimelineEventRequest
        {
            Id = Id,
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
            Snackbar.Add($"Failed to save event: {response.Error}", Severity.Error);
            return;
        }

        ApiResponse<RelatedItemsDto> relResponse = await RelatedItemsService.SetForAsync(
            RelatedItemKind.TimelineEvent, Id, BuildRelatedRequest());
        _saving = false;

        if (!relResponse.Success)
        {
            Snackbar.Add($"Saved event, but failed to save related items: {relResponse.Error}", Severity.Warning);
            return;
        }

        Snackbar.Add("Event saved.", Severity.Success);
        Navigation.NavigateTo("/timeline");
    }

    private SetRelatedItemsRequest BuildRelatedRequest() => new()
    {
        CharacterIds = _related.Characters.Select(r => r.Id).ToList(),
        LogIds = _related.Logs.Select(r => r.Id).ToList(),
        TimelineEventIds = _related.TimelineEvents.Select(r => r.Id).ToList(),
    };

    private async Task DeleteAsync()
    {
        bool? confirm = await Dialog.ShowMessageBoxAsync(
            "Delete event",
            $"Delete '{_title}'? This cannot be undone.",
            "Delete", cancelText: "Cancel");

        if (confirm != true) return;

        ApiResponse<bool> response = await TimelineService.DeleteAsync(Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to delete: {response.Error}", Severity.Error);
            return;
        }

        Snackbar.Add("Event deleted.", Severity.Success);
        Navigation.NavigateTo("/timeline");
    }
}
