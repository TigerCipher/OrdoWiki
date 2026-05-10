namespace OrdoWiki.Web.Components.Pages.Tags;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class TagIndex
{
    private IReadOnlyList<TagDto> _tags = [];
    private List<TagDto> _filtered = [];
    private string _search = string.Empty;
    private bool _loading = true;

    [Inject]
    private ITagService TagService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        _tags = await TagService.GetAllWithCountsAsync();
        _filtered = _tags.ToList();
        _loading = false;
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(_search))
        {
            _filtered = _tags.ToList();
            return;
        }

        string term = _search.Trim();
        _filtered = _tags
            .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || t.Slug.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
