namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrdoWiki.Data.Entities;

public partial class RelatedItemPicker
{
    private readonly HashSet<Guid> _selectedIds = [];
    private List<RelatedItemRef> _selected = [];
    private MudAutocomplete<RelatedItemRef>? _input;

    [Parameter, EditorRequired]
    public RelatedItemKind Kind { get; set; }

    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public string Placeholder { get; set; } = "Search…";

    [Parameter]
    public IReadOnlyList<RelatedItemRef> Value { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyList<RelatedItemRef>> ValueChanged { get; set; }

    // Optional excluded id (the current entity) so it can't relate to itself.
    [Parameter]
    public Guid? ExcludeId { get; set; }

    [Inject]
    private IRelatedItemsService RelatedItems { get; set; } = null!;

    protected override void OnParametersSet()
    {
        if (Value.Count == _selected.Count && Value.Select(v => v.Id).SequenceEqual(_selected.Select(s => s.Id)))
            return;

        _selectedIds.Clear();
        _selected = [];
        foreach (RelatedItemRef item in Value)
        {
            if (_selectedIds.Add(item.Id)) _selected.Add(item);
        }
    }

    private async Task<IEnumerable<RelatedItemRef>> SearchAsync(string? query, CancellationToken ct)
    {
        IReadOnlyList<RelatedItemRef> results = await RelatedItems.SearchAsync(Kind, query);
        return results
            .Where(r => !_selectedIds.Contains(r.Id))
            .Where(r => !ExcludeId.HasValue || r.Id != ExcludeId.Value);
    }

    private async Task OnPickedAsync(RelatedItemRef? value)
    {
        if (value is null) return;
        if (!_selectedIds.Add(value.Id)) return;

        _selected.Add(value);
        if (_input is not null) await _input.ClearAsync();
        await ValueChanged.InvokeAsync(_selected.AsReadOnly());
    }

    private async Task RemoveAsync(RelatedItemRef item)
    {
        if (!_selectedIds.Remove(item.Id)) return;
        _selected.Remove(item);
        await ValueChanged.InvokeAsync(_selected.AsReadOnly());
    }
}
