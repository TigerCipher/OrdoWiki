namespace OrdoWiki.Web.Components.Pages.Wiki;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class EditLog
{
    private WikiPageDto _page = new();
    private PageRevisionDto _revision = new();
    private bool _loading = true;
    private string _originalTitle = string.Empty;

    private string _editSummary = string.Empty;
    private string _markdownBody = string.Empty;
    private ContentFormat _format = ContentFormat.Markdown;
    private IReadOnlyList<string> _tagNames = [];
    private RelatedItemsDto _related = new();

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        ApiResponse<WikiPageDto> response = await PageService.GetPageBySlugAsync(Slug);

        if (!response)
        {
            Snackbar.Add($"Failed to load page - {response.Error}", Severity.Error);
            Navigation.NavigateTo("/not-found");
            return;
        }

        _page = response;
        _revision = _page.CurrentRevision ?? new PageRevisionDto();
        _markdownBody = _revision.MarkdownBody;
        _format = _revision.ContentFormat;
        _originalTitle = _page.Title;
        _tagNames = _page.Tags.Select(t => t.Name).ToList();
        _related = await RelatedItemsService.GetForAsync(RelatedItemKind.Log, _page.Id);
        _loading = false;
    }

    private void OnTagsChanged(IReadOnlyList<string> tags) => _tagNames = tags;

    private async Task SaveEditAsync()
    {
        ApiResponse<WikiPageDto> response = await PageService.EditPageAsync(new EditPageRequest
        {
            PageId = _page.Id,
            Title = _page.Title,
            MarkdownBody = _markdownBody,
            ContentFormat = _format,
            EditSummary = _editSummary,
            Slug = _page.Slug,
            Summary = _page.Summary,
            Tags = _tagNames,
        });

        if (!response)
        {
            Snackbar.Add($"Failed to save page - {response.Error}", Severity.Error);
            return;
        }

        ApiResponse<RelatedItemsDto> relResponse = await RelatedItemsService.SetForAsync(
            RelatedItemKind.Log, _page.Id, BuildRelatedRequest());

        if (!relResponse.Success)
        {
            Snackbar.Add($"Saved page, but failed to save related items: {relResponse.Error}", Severity.Warning);
            return;
        }

        Snackbar.Add("Page saved", Severity.Success);
    }

    private SetRelatedItemsRequest BuildRelatedRequest() => new()
    {
        CharacterIds = _related.Characters.Select(r => r.Id).ToList(),
        LogIds = _related.Logs.Select(r => r.Id).ToList(),
        TimelineEventIds = _related.TimelineEvents.Select(r => r.Id).ToList(),
    };
}
