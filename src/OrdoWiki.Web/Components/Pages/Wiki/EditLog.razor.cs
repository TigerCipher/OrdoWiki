namespace OrdoWiki.Web.Components.Pages.Wiki;

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

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

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
        _originalTitle = _page.Title;
        _loading = false;
    }

    private async Task SaveEditAsync()
    {
        ApiResponse<WikiPageDto> response = await PageService.EditPageAsync(new EditPageRequest
        {
            PageId = _page.Id,
            Title = _page.Title,
            MarkdownBody = _markdownBody,
            EditSummary = _editSummary,
            Slug = _page.Slug,
            Summary = _page.Summary
        });

        if (!response)
        {
            Snackbar.Add($"Failed to save page - {response.Error}", Severity.Error);
        }
        else
        {
            Snackbar.Add("Page saved", Severity.Success);
        }
    }
}