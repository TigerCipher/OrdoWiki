namespace OrdoWiki.Web.Components.Pages.Characters;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class CharacterList
{
    private const int OwnersPerPage = 10;
    private const int CharactersPerRow = 10;

    private List<CharacterGroup> _allGroups = [];
    private List<CharacterGroup> _filteredGroups = [];
    private List<CharacterGroup> _pageGroups = [];
    private readonly HashSet<string> _expandedOwners = new(StringComparer.Ordinal);
    private bool _loading = true;
    private string _search = string.Empty;
    private int _page = 1;
    private int _pageCount = 1;

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<List<CharacterDto>> response = await CharacterService.GetCharactersAsync();
        _loading = false;

        if (!response)
        {
            Snackbar.Add($"Failed to load characters: {response.Error}", Severity.Error);
            return;
        }

        _allGroups = response.Value
            .GroupBy(c => c.OwnerId)
            .Select(g => new CharacterGroup(
                g.First().Owner,
                g.OrderBy(c => c.Name).ToList(),
                CapRow: true))
            .OrderBy(g => g.Owner?.DisplayName ?? g.Owner?.Username ?? string.Empty)
            .ToList();

        ApplyFilter();
    }

    private void OnSearchChanged()
    {
        _page = 1;
        // Filter contents change with the search term, so any "expanded" state
        // from the previous query no longer maps cleanly. Reset it.
        _expandedOwners.Clear();
        ApplyFilter();
    }

    private void OnPageChanged(int page)
    {
        _page = page;
        ApplyPage();
    }

    private void ToggleExpanded(string ownerId)
    {
        if (!_expandedOwners.Add(ownerId))
            _expandedOwners.Remove(ownerId);
    }

    private void ApplyFilter()
    {
        string term = (_search ?? string.Empty).Trim();
        if (term.Length == 0)
        {
            _filteredGroups = _allGroups;
        }
        else
        {
            _filteredGroups = _allGroups
                .Select(g =>
                {
                    bool ownerMatches =
                        Contains(g.Owner?.DisplayName, term) ||
                        Contains(g.Owner?.Username, term);

                    if (ownerMatches) return g;

                    List<CharacterDto> matched = g.Characters
                        .Where(c => Contains(c.Name, term))
                        .ToList();

                    // When the search hits a character (not the owner), surface
                    // every matching character regardless of how deep in the
                    // owner's roster it sits — so don't apply the row cap.
                    return matched.Count > 0
                        ? new CharacterGroup(g.Owner, matched, CapRow: false)
                        : null;
                })
                .Where(g => g is not null)
                .Select(g => g!)
                .ToList();
        }

        _pageCount = Math.Max(1, (int)Math.Ceiling(_filteredGroups.Count / (double)OwnersPerPage));
        if (_page > _pageCount) _page = _pageCount;
        ApplyPage();
    }

    private void ApplyPage()
    {
        _pageGroups = _filteredGroups
            .Skip((_page - 1) * OwnersPerPage)
            .Take(OwnersPerPage)
            .ToList();
    }

    private List<CharacterDto> VisibleCharacters(CharacterGroup group)
    {
        if (!group.CapRow) return group.Characters;
        if (group.Owner is null) return group.Characters;
        if (_expandedOwners.Contains(group.Owner.Id)) return group.Characters;
        if (group.Characters.Count <= CharactersPerRow) return group.Characters;
        return group.Characters.Take(CharactersPerRow).ToList();
    }

    private bool ShowExpandLink(CharacterGroup group) =>
        group.CapRow
        && group.Owner is not null
        && group.Characters.Count > CharactersPerRow;

    private bool IsExpanded(CharacterGroup group) =>
        group.Owner is not null && _expandedOwners.Contains(group.Owner.Id);

    private static bool Contains(string? source, string term) =>
        !string.IsNullOrEmpty(source) &&
        source.Contains(term, StringComparison.OrdinalIgnoreCase);

    private sealed record CharacterGroup(UserDto? Owner, List<CharacterDto> Characters, bool CapRow);
}
