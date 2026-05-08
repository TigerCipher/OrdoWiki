namespace OrdoWiki.Web.Components.Pages.Characters;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class CharacterCreate
{
    private string _name = string.Empty;
    private string _slug = string.Empty;
    private string _summary = string.Empty;
    private string _body = string.Empty;
    private bool _loading = true;
    private bool _canCreate;
    private bool _saving;

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<bool> response = await CharacterService.CanCreateCharacterAsync();
        _canCreate = response.Success && response.Value;
        _loading = false;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            Snackbar.Add("Name is required", Severity.Error);
            return;
        }

        _saving = true;
        try
        {
            ApiResponse<CharacterDto> response = await CharacterService.CreateCharacterAsync(new CreateCharacterRequest
            {
                Name = _name,
                Summary = string.IsNullOrWhiteSpace(_summary) ? null : _summary,
                MarkdownBody = _body,
                Slug = string.IsNullOrWhiteSpace(_slug) ? null : _slug,
            });

            if (!response)
            {
                Snackbar.Add($"Failed to create character: {response.Error}", Severity.Error);
                return;
            }

            Snackbar.Add("Character created", Severity.Success);
            Navigation.NavigateTo($"/characters/{response.Value.Slug}/edit");
        }
        finally
        {
            _saving = false;
        }
    }
}
