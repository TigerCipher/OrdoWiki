namespace OrdoWiki.Web.Components.Pages.Wiki;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class LogRevisionViewer
{
    private WikiPageDto _page = new();
    private PageRevisionDto _revision = new();
    private string _renderedHtml = string.Empty;
    private bool _isCurrent;
    private bool _loading = true;

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Parameter, EditorRequired]
    public Guid RevisionId { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IContentRenderer Content { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        ApiResponse<WikiPageDto> pageResponse = await PageService.GetPageBySlugAsync(Slug);
        if (!pageResponse)
        {
            Snackbar.Add($"Failed to load page - {pageResponse.Error}", Severity.Error);
            Navigation.NavigateTo("/not-found");
            return;
        }

        _page = pageResponse;

        ApiResponse<PageRevisionDto> revisionResponse = await PageService.GetRevisionAsync(RevisionId);
        if (!revisionResponse)
        {
            Snackbar.Add($"Failed to load revision - {revisionResponse.Error}", Severity.Error);
            Navigation.NavigateTo($"/logs/{Slug}/history");
            return;
        }

        _revision = revisionResponse;
        if (_revision.PageId != _page.Id)
        {
            Snackbar.Add("Revision does not belong to this page.", Severity.Error);
            Navigation.NavigateTo($"/logs/{Slug}/history");
            return;
        }

        _renderedHtml = Content.Render(_revision.ContentFormat, _revision.MarkdownBody);
        _isCurrent = _page.CurrentRevisionId == _revision.Id;
        _loading = false;
    }
}
