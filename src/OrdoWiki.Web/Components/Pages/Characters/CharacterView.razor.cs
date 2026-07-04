namespace OrdoWiki.Web.Components.Pages.Characters;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class CharacterView
{
    private CharacterDto _character = new();
    private string _renderedHtml = string.Empty;
    private bool _canEdit;
    private bool _loading = true;
    private RelatedItemsDto _related = new();

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private IContentRenderer Content { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnParametersSetAsync()
    {
        ApiResponse<CharacterDto> response = await CharacterService.GetCharacterBySlugAsync(Slug);

        if (!response)
        {
            Snackbar.Add($"Failed to load character: {response.Error}", Severity.Error);
            Navigation.NavigateTo("/not-found");
            return;
        }

        _character = response;
        _renderedHtml = Content.Render(_character.ContentFormat, _character.MarkdownBody);
        _related = await RelatedItemsService.GetForAsync(RelatedItemKind.Character, _character.Id);

        ApiResponse<bool> editCheck = await CharacterService.CanEditCharacterAsync(_character.Id);
        _canEdit = editCheck.Success && editCheck.Value;

        _loading = false;
    }
}
