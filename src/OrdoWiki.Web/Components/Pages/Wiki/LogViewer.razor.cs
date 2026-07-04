namespace OrdoWiki.Web.Components.Pages.Wiki;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Dialogs;

public partial class LogViewer
{
    private WikiPageDto _page = new();
    private PageRevisionDto _revision = new();
    private string _renderedHtml = string.Empty;
    private bool _showEditedRow;
    private bool _loading = true;
    private RelatedItemsDto _related = new();

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private IOrdoDialogs Dialogs { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IContentRenderer Content { get; set; } = null!;

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
        _renderedHtml = Content.Render(_revision.ContentFormat, _revision.MarkdownBody);

        // Created+initial-revision are written within milliseconds of each other; only
        // show the "last edited" row when the page has been edited after creation.
        _showEditedRow = _revision.EditedAt - _page.CreatedAt > TimeSpan.FromSeconds(5);
        _related = await RelatedItemsService.GetForAsync(RelatedItemKind.Log, _page.Id);

        _loading = false;
    }
}
