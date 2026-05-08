namespace OrdoWiki.Web.Components.Pages.Characters;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class CharacterList
{
    private List<CharacterGroup> _groups = [];
    private bool _loading = true;

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

        _groups = response.Value
            .GroupBy(c => c.OwnerId)
            .Select(g => new CharacterGroup(
                g.First().Owner,
                g.OrderBy(c => c.Name).ToList()))
            .OrderBy(g => g.Owner?.DisplayName ?? g.Owner?.Username ?? string.Empty)
            .ToList();
    }

    private sealed record CharacterGroup(UserDto? Owner, List<CharacterDto> Characters);
}
