namespace OrdoWiki.Web.Components.Pages.Wiki;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Dialogs;

public partial class LogHistory
{
    private WikiPageDto _page = new();
    private List<PageRevisionDto> _revisions = [];
    private readonly HashSet<Guid> _selected = [];
    private bool _loading = true;

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

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

        ApiResponse<List<PageRevisionDto>> revisionsResponse = await PageService.GetRevisionsAsync(_page.Id);
        if (!revisionsResponse)
        {
            Snackbar.Add($"Failed to load revisions - {revisionsResponse.Error}", Severity.Error);
            _revisions = [];
        }
        else
        {
            _revisions = revisionsResponse;
        }

        _loading = false;
    }

    private void ToggleSelected(Guid revisionId, bool isChecked)
    {
        if (isChecked)
        {
            // Cap selection at two — drop the oldest pick to make room.
            if (_selected.Count >= 2)
                _selected.Remove(_selected.First());
            _selected.Add(revisionId);
        }
        else
        {
            _selected.Remove(revisionId);
        }
    }

    private void CompareSelected()
    {
        if (_selected.Count != 2) return;

        // Order picks so the older revision is "from" and the newer is "to".
        List<PageRevisionDto> picks = _revisions
            .Where(r => _selected.Contains(r.Id))
            .OrderBy(r => r.EditedAt)
            .ToList();

        Navigation.NavigateTo($"/logs/{_page.Slug}/compare?from={picks[0].Id}&to={picks[1].Id}");
    }

    private string BuildDiffHrefAgainstCurrent(Guid revisionId)
    {
        Guid? current = _page.CurrentRevisionId;
        if (current is null) return $"/logs/{_page.Slug}/history/{revisionId}";
        return $"/logs/{_page.Slug}/compare?from={revisionId}&to={current}";
    }

    private async Task RestoreAsync(PageRevisionDto revision)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.Title), "Restore this revision?" },
            { nameof(ConfirmDialog.Message),
                $"This will write a new revision with the body from {revision.EditedAt:yyyy-MM-dd HH:mm} UTC and make it the current version. The existing history is preserved." },
            { nameof(ConfirmDialog.PrimaryText), "Restore" },
            { nameof(ConfirmDialog.PrimaryColor), Color.Warning },
            { nameof(ConfirmDialog.CancelText), "Cancel" },
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>("Restore", parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled) return;

        ApiResponse<WikiPageDto> response = await PageService.RestoreRevisionAsync(revision.Id);
        if (!response)
        {
            Snackbar.Add($"Failed to restore - {response.Error}", Severity.Error);
            return;
        }

        Snackbar.Add("Revision restored", Severity.Success);
        Navigation.NavigateTo($"/logs/{_page.Slug}");
    }
}
