namespace OrdoWiki.Web.Components.Pages.Timeline;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class TimelineViewer
{
    private TimelineEventDto _event = new();
    private string _renderedHtml = string.Empty;
    private bool _showEditedRow;
    private bool _loading = true;
    private RelatedItemsDto _related = new();

    [Parameter, EditorRequired]
    public required Guid Id { get; set; }

    [Inject]
    private ITimelineService TimelineService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private IContentRenderer Content { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<TimelineEventDto> response = await TimelineService.GetEventByIdAsync(Id);
        if (!response)
        {
            Snackbar.Add($"Failed to load event - {response.Error}", Severity.Error);
            Navigation.NavigateTo("/not-found");
            return;
        }

        _event = response;
        _renderedHtml = Content.Render(_event.ContentFormat, _event.MarkdownBody);
        _showEditedRow = _event.UpdatedAt - _event.CreatedAt > TimeSpan.FromSeconds(5);
        _related = await RelatedItemsService.GetForAsync(RelatedItemKind.TimelineEvent, Id);
        _loading = false;
    }
}
