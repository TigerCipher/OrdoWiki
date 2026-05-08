namespace OrdoWiki.Web.Components.Pages.Characters;

using Data.Auth;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class MyCharacters
{
    private List<CharacterDto> _characters = [];
    private bool _loading = true;
    private bool _canCreate;
    private string? _capInfo;

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        // The "New character" button lives in a <SectionContent> that renders into
        // an <SectionOutlet> in the SSR MainLayout. SSR outlets only paint once, so
        // the cap check has to run during prerender — otherwise the button's initial
        // Disabled state is wrong and no later StateHasChanged can fix it.
        ApiResponse<bool> canCreate = await CharacterService.CanCreateCharacterAsync();
        _canCreate = canCreate is { Success: true, Value: true };
        if (!_canCreate)
            _capInfo = $"You've reached the limit of {CharacterCaps.ReaderMaxCharacters} characters. Delete one to make room for another.";

        ApiResponse<List<CharacterDto>> response = await CharacterService.GetMyCharactersAsync();
        if (!response)
        {
            _loading = false;
            if (RendererInfo.IsInteractive)
                Snackbar.Add($"Failed to load your characters: {response.Error}", Severity.Error);
            return;
        }

        _characters = response;
        _loading = false;
    }
}
