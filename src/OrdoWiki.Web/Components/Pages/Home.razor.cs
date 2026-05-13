namespace OrdoWiki.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrdoWiki.Web.Models.Requests;

public partial class Home
{
    private HomePageDto _home = new();
    private bool _loading = true;
    private bool _saving;

    private bool _editingBio;
    private string _bioDraft = string.Empty;

    private bool _editingFeatured;
    private WikiPageDto? _featuredDraft;
    private List<WikiPageDto> _allPages = [];

    [Inject]
    private IHomePageService HomePageService { get; set; } = null!;

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _home = await HomePageService.GetAsync();
        _loading = false;
    }

    private void StartEditBio()
    {
        _bioDraft = _home.BioMarkdown;
        _editingBio = true;
    }

    private void CancelBio()
    {
        _editingBio = false;
        _bioDraft = string.Empty;
    }

    private async Task SaveBioAsync()
    {
        _saving = true;
        ApiResponse<HomePageDto> response = await HomePageService.UpdateBioAsync(
            new UpdateBioRequest { BioMarkdown = _bioDraft });
        _saving = false;

        if (!response)
        {
            Snackbar.Add($"Failed to save bio: {response.Error}", Severity.Error);
            return;
        }

        _home = response.Value;
        _editingBio = false;
        _bioDraft = string.Empty;
        Snackbar.Add("Bio updated.", Severity.Success);
    }

    private async Task StartEditFeatured()
    {
        if (_allPages.Count == 0)
        {
            ApiResponse<List<WikiPageDto>> pages = await PageService.GetPagesAsync();
            if (pages.Success) _allPages = pages.Value;
        }

        _featuredDraft = _home.FeaturedLogId is { } id
            ? _allPages.FirstOrDefault(p => p.Id == id)
            : null;
        _editingFeatured = true;
    }

    private void CancelFeatured()
    {
        _editingFeatured = false;
        _featuredDraft = null;
    }

    private async Task SaveFeaturedAsync()
    {
        _saving = true;
        ApiResponse<HomePageDto> response = await HomePageService.SetFeaturedLogAsync(
            new SetFeaturedLogRequest { FeaturedLogId = _featuredDraft?.Id });
        _saving = false;

        if (!response)
        {
            Snackbar.Add($"Failed to save featured log: {response.Error}", Severity.Error);
            return;
        }

        _home = response.Value;
        _editingFeatured = false;
        _featuredDraft = null;
        Snackbar.Add("Featured log updated.", Severity.Success);
    }

    private Task<IEnumerable<WikiPageDto>> SearchPagesAsync(string? value, CancellationToken cancellationToken)
    {
        IEnumerable<WikiPageDto> matches = string.IsNullOrWhiteSpace(value)
            ? _allPages
            : _allPages.Where(p => p.Title.Contains(value, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(matches);
    }
}
