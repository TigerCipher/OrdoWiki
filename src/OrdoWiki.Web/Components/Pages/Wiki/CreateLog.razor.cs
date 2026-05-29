namespace OrdoWiki.Web.Components.Pages.Wiki;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class CreateLog
{
    private string _title = string.Empty;
    private string _slug = string.Empty;
    private string _summary = string.Empty;
    private string _content = string.Empty;
    private RelatedItemsDto _related = new();

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

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
            return;
        }

        if (!_related.IsEmpty)
        {
            await RelatedItemsService.SetForAsync(
                RelatedItemKind.Log,
                response.Value.Id,
                new SetRelatedItemsRequest
                {
                    CharacterIds = _related.Characters.Select(r => r.Id).ToList(),
                    LogIds = _related.Logs.Select(r => r.Id).ToList(),
                    TimelineEventIds = _related.TimelineEvents.Select(r => r.Id).ToList(),
                });
        }

        Snackbar.Add("Page created", Severity.Success);
        Navigation.NavigateTo($"/logs/{response.Value.Slug}");
    }
}
