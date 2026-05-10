namespace OrdoWiki.Web.Components.Pages.Wiki;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Services;
using Shared.Dialogs;

public partial class Page
{
    private List<WikiPageDto> _pages = [];
    private IReadOnlyList<TagDto> _allTags = [];
    private TagDto? _selectedTag;
    private int _rowsPerPage = 10;
    private bool _loading = true;

    [SupplyParameterFromQuery(Name = "tag")]
    public string? TagSlug { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private ITagService TagService { get; set; } = null!;

    [Inject]
    private IOrdoDialogs OrdoDialogs { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ITimeZoneService Tz { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        _allTags = await TagService.GetAllAsync();
        _selectedTag = string.IsNullOrEmpty(TagSlug)
            ? null
            : _allTags.FirstOrDefault(t => string.Equals(t.Slug, TagSlug, StringComparison.OrdinalIgnoreCase));

        await LoadPagesAsync();
    }

    private async Task LoadPagesAsync()
    {
        _loading = true;
        ApiResponse<List<WikiPageDto>> response = await PageService.GetPagesAsync(_selectedTag?.Id);
        _loading = false;

        if (!response)
        {
            IDialogReference dialog = await OrdoDialogs.ShowErrorAsync($"Failed to load pages - {response.Error}");
            await dialog.Result;
            Navigation.NavigateTo("/");
            return;
        }

        TimeZoneInfo userTz = await Tz.GetLocalAsync();
        foreach (WikiPageDto page in response.Value)
        {
            page.CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(page.CreatedAt, DateTimeKind.Utc),
                userTz);

            page.CurrentRevision?.EditedAt = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(page.CurrentRevision.EditedAt, DateTimeKind.Utc),
                userTz);
        }

        _pages = response;
    }

    private async Task OnTagFilterChangedAsync(TagDto? tag)
    {
        _selectedTag = tag;
        string url = tag is null
            ? Navigation.GetUriWithQueryParameter("tag", (string?)null)
            : Navigation.GetUriWithQueryParameter("tag", tag.Slug);
        Navigation.NavigateTo(url, replace: true);
        await LoadPagesAsync();
    }

    private void OpenPage(DataGridRowClickEventArgs<WikiPageDto> args)
    {
        Navigation.NavigateTo($"/logs/{args.Item.Slug}");
    }

    private void CreatePage()
    {
        Navigation.NavigateTo("/logs/new");
    }

    private void EditPage(WikiPageDto item)
    {
        Navigation.NavigateTo($"/logs/{item.Slug}/edit");
    }
}
