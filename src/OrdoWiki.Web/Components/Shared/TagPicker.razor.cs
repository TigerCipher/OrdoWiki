namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

public partial class TagPicker
{
    private readonly HashSet<string> _selectedSet = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _selected = [];
    private MudAutocomplete<string>? _input;
    private string _typedText = string.Empty;

    [Parameter]
    public IReadOnlyList<string> Value { get; set; } = [];

    [Parameter]
    public EventCallback<IReadOnlyList<string>> ValueChanged { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Inject]
    private ITagService TagService { get; set; } = null!;

    protected override void OnParametersSet()
    {
        // Sync internal selection with the parent-supplied list whenever it differs.
        if (Value.SequenceEqual(_selected, StringComparer.OrdinalIgnoreCase)) return;

        _selectedSet.Clear();
        _selected = [];
        foreach (string name in Value)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            string trimmed = name.Trim();
            if (_selectedSet.Add(trimmed)) _selected.Add(trimmed);
        }
    }

    private async Task<IEnumerable<string>> SearchAsync(string query, CancellationToken ct)
    {
        IReadOnlyList<TagDto> tags = await TagService.SearchAsync(query ?? string.Empty);
        IEnumerable<string> matches = tags
            .Select(t => t.Name)
            .Where(n => !_selectedSet.Contains(n));

        // Offer "create" affordance when the typed text doesn't match any existing tag.
        string trimmed = (query ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(trimmed)
            && !tags.Any(t => string.Equals(t.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            && !_selectedSet.Contains(trimmed))
        {
            matches = matches.Append($"+ Create \"{trimmed}\"");
        }

        return matches;
    }

    private async Task OnPickedAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        string name = value.StartsWith("+ Create \"") && value.EndsWith("\"")
            ? value[10..^1]
            : value;

        await AddAsync(name);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        // Enter on a typed-but-not-picked value commits as a new tag.
        if (e.Key != "Enter") return;
        if (string.IsNullOrWhiteSpace(_typedText)) return;
        await AddAsync(_typedText);
    }

    private async Task AddAsync(string name)
    {
        string trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        if (!_selectedSet.Add(trimmed)) return;

        _selected.Add(trimmed);
        _typedText = string.Empty;
        if (_input is not null) await _input.ClearAsync();
        await ValueChanged.InvokeAsync(_selected.AsReadOnly());
    }

    private async Task RemoveAsync(string name)
    {
        if (!_selectedSet.Remove(name)) return;
        _selected.Remove(name);
        await ValueChanged.InvokeAsync(_selected.AsReadOnly());
    }
}
