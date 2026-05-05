namespace OrdoWiki.Web.Components.Pages.Wiki;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Dialogs;

public partial class LogViewer
{
    private WikiPageDto _page = new();
    private PageRevisionDto _revision = new();
    private string _renderedHtml = string.Empty;

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IOrdoDialogs Dialogs { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IMarkdownService Markdown { get; set; } = null!;

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
        _renderedHtml = Markdown.Render(_revision.MarkdownBody);
    }
}