namespace OrdoWiki.Web.Components.Pages.Wiki;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class CreateLog
{
    private string _title = string.Empty;
    private string _slug = string.Empty;
    private string _summary = string.Empty;
    private string _content = string.Empty;

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private async Task SaveEditAsync()
    {
        if (string.IsNullOrWhiteSpace(_title))
        {
            Snackbar.Add("Title is required", Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(_content))
        {
            Snackbar.Add("Content is required", Severity.Error);
            return;
        }
        
        string? slug = string.IsNullOrWhiteSpace(_slug) ? null : _slug;
        string? summary = string.IsNullOrWhiteSpace(_summary) ? null : _summary;

        ApiResponse<WikiPageDto> response = await PageService.CreatePageAsync(new CreatePageRequest
        {
            Title = _title,
            MarkdownBody = _content,
            Summary = summary,
            Slug = slug
        });
        
        if (!response)
        {
            Snackbar.Add($"Failed to create page - {response.Error}", Severity.Error);
        }
        else
        {
            Snackbar.Add("Page created", Severity.Success);
        }
    }
}