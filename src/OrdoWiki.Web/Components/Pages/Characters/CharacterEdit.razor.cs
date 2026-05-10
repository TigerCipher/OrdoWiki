namespace OrdoWiki.Web.Components.Pages.Characters;

using Data.Auth;
using Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Web.Models.Requests;

public partial class CharacterEdit
{
    private CharacterDto _character = new();
    private string _originalName = string.Empty;
    private bool _loading = true;
    private bool _canEdit;
    private bool _saving;
    private int? _imageCap;
    private IReadOnlyList<string> _tagNames = [];

    [Parameter, EditorRequired]
    public required string Slug { get; set; }

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthProvider { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

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
        _originalName = _character.Name;
        _tagNames = _character.Tags.Select(t => t.Name).ToList();

        ApiResponse<bool> editCheck = await CharacterService.CanEditCharacterAsync(_character.Id);
        _canEdit = editCheck.Success && editCheck.Value;

        AuthenticationState auth = await AuthProvider.GetAuthenticationStateAsync();
        bool isPrivileged = auth.User.IsAdmin() || auth.User.IsDesigner() || auth.User.IsEditor();
        _imageCap = isPrivileged ? null : CharacterCaps.ReaderMaxImagesPerCharacter;

        _loading = false;
    }

    private void OnTagsChanged(IReadOnlyList<string> tags) => _tagNames = tags;

    private async Task SaveAsync()
    {
        _saving = true;
        try
        {
            ApiResponse<CharacterDto> response = await CharacterService.EditCharacterAsync(new EditCharacterRequest
            {
                CharacterId = _character.Id,
                Name = _character.Name,
                Summary = _character.Summary,
                MarkdownBody = _character.MarkdownBody,
                Slug = _character.Slug,
                Tags = _tagNames,
            });

            if (!response)
            {
                Snackbar.Add($"Failed to save: {response.Error}", Severity.Error);
                return;
            }

            Snackbar.Add("Character saved", Severity.Success);

            if (response.Value.Slug != Slug)
            {
                Navigation.NavigateTo($"/characters/{response.Value.Slug}/edit");
                return;
            }

            _character = response;
            _originalName = _character.Name;
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task DeleteAsync()
    {
        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Delete character",
            $"Permanently delete '{_character.Name}'? This will also remove its gallery rows (the underlying images stay in the asset library).",
            yesText: "Delete",
            cancelText: "Cancel");

        if (confirm != true) return;

        ApiResponse<bool> response = await CharacterService.DeleteCharacterAsync(_character.Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to delete: {response.Error}", Severity.Error);
            return;
        }

        Snackbar.Add("Character deleted", Severity.Success);
        Navigation.NavigateTo("/characters/mine");
    }
}
