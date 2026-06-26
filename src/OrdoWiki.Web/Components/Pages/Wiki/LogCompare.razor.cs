namespace OrdoWiki.Web.Components.Pages.Wiki;

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class LogCompare
{
    private WikiPageDto _page = new();
    private PageRevisionDto? _from;
    private PageRevisionDto? _to;
    private SideBySideDiffModel? _diff;
    private bool _hasChanges;
    private bool _loading = true;
    private bool _initialized;

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [SupplyParameterFromQuery(Name = "from")]
    public Guid? FromId { get; set; }

    [SupplyParameterFromQuery(Name = "to")]
    public Guid? ToId { get; set; }

    [Inject]
    private IPageService PageService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        // Blazor fires OnParametersSetAsync once more as the user navigates away
        // from this component — by then the query params are gone, which would
        // otherwise spam a "missing IDs" snackbar on every exit. Only react to the
        // first invocation; subsequent param changes (e.g. swapping which two
        // revisions to compare) intentionally remount via NavigateTo.
        if (_initialized) return;
        _initialized = true;

        if (FromId is null || ToId is null)
        {
            Snackbar.Add("Missing revision IDs to compare.", Severity.Error);
            Navigation.NavigateTo($"/logs/{Slug}/history");
            return;
        }

        ApiResponse<WikiPageDto> pageResponse = await PageService.GetPageBySlugAsync(Slug);
        if (!pageResponse)
        {
            Snackbar.Add($"Failed to load page - {pageResponse.Error}", Severity.Error);
            Navigation.NavigateTo("/not-found");
            return;
        }

        _page = pageResponse;

        ApiResponse<PageRevisionDto> fromResponse = await PageService.GetRevisionAsync(FromId.Value);
        ApiResponse<PageRevisionDto> toResponse = await PageService.GetRevisionAsync(ToId.Value);

        if (!fromResponse || !toResponse)
        {
            Snackbar.Add("One or both revisions could not be loaded.", Severity.Error);
            Navigation.NavigateTo($"/logs/{Slug}/history");
            return;
        }

        _from = fromResponse;
        _to = toResponse;

        if (_from.PageId != _page.Id || _to.PageId != _page.Id)
        {
            Snackbar.Add("Revisions don't belong to this page.", Severity.Error);
            Navigation.NavigateTo($"/logs/{Slug}/history");
            return;
        }

        _diff = SideBySideDiffBuilder.Diff(_from.MarkdownBody ?? string.Empty, _to.MarkdownBody ?? string.Empty);
        _hasChanges = _diff.OldText.Lines.Concat(_diff.NewText.Lines)
            .Any(l => l.Type is ChangeType.Inserted or ChangeType.Deleted or ChangeType.Modified);

        _loading = false;
    }

    private static string CellClass(ChangeType type) => type switch
    {
        ChangeType.Inserted => "diff-cell diff-cell-inserted",
        ChangeType.Deleted => "diff-cell diff-cell-deleted",
        ChangeType.Modified => "diff-cell diff-cell-modified",
        ChangeType.Imaginary => "diff-cell diff-cell-imaginary",
        _ => "diff-cell",
    };

    private static string RenderLineNumber(DiffPiece line) =>
        line.Position is { } pos ? pos.ToString() : string.Empty;

    private static MarkupString RenderLine(DiffPiece line)
    {
        if (line.Type == ChangeType.Imaginary)
            return new MarkupString(string.Empty);

        // Modified lines carry per-word SubPieces; render those with inline marks so
        // the eye can find the exact tokens that changed instead of just the row.
        if (line.Type == ChangeType.Modified && line.SubPieces.Count > 0)
        {
            System.Text.StringBuilder sb = new();
            foreach (DiffPiece piece in line.SubPieces)
            {
                string escaped = System.Net.WebUtility.HtmlEncode(piece.Text ?? string.Empty);
                string cssClass = piece.Type switch
                {
                    ChangeType.Inserted => "diff-word-inserted",
                    ChangeType.Deleted => "diff-word-deleted",
                    _ => string.Empty,
                };
                if (string.IsNullOrEmpty(cssClass))
                    sb.Append(escaped);
                else
                    sb.Append($"<span class=\"{cssClass}\">{escaped}</span>");
            }
            return new MarkupString(sb.ToString());
        }

        return new MarkupString(System.Net.WebUtility.HtmlEncode(line.Text ?? string.Empty));
    }
}
