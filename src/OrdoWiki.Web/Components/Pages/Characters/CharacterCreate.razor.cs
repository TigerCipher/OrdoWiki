namespace OrdoWiki.Web.Components.Pages.Characters;

using Data.Auth;
using Data.Entities;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models.Requests;

public partial class CharacterCreate
{
    // Generated up front so images uploaded inside the editor attach to this
    // character from the very first upload, instead of being orphaned standalone.
    private readonly Guid _characterId = Guid.NewGuid();
    private string _name = string.Empty;
    private string _slug = string.Empty;
    private string _summary = string.Empty;
    private string _body = string.Empty;
    private ContentFormat _format = ContentFormat.Html;
    private bool _loading = true;
    private bool _canCreate;
    private bool _saving;
    private bool _canAssignOwner;
    private UserDto? _owner;
    private RelatedItemsDto _related = new();

    [Inject]
    private ICharacterService CharacterService { get; set; } = null!;

    [Inject]
    private IUserService UserService { get; set; } = null!;

    [Inject]
    private IRelatedItemsService RelatedItemsService { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        ApiResponse<bool> response = await CharacterService.CanCreateCharacterAsync();
        _canCreate = response.Success && response.Value;

        ApiResponse<UserDto> me = await UserService.GetCurrentUserAsync();
        if (me.Success)
        {
            _owner = me.Value;
            _canAssignOwner =
                string.Equals(me.Value.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(me.Value.Role, Roles.Designer, StringComparison.OrdinalIgnoreCase);
        }

        _loading = false;
    }

    private Task<IEnumerable<UserDto>> SearchOwnersAsync(string? value, CancellationToken cancellationToken)
        => SearchUsersAsync(value);

    private async Task<IEnumerable<UserDto>> SearchUsersAsync(string? value)
    {
        List<UserDto> matches = await UserService.SearchUsersAsync(value);
        return matches;
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
                Id = _characterId,
                Name = _name,
                Summary = string.IsNullOrWhiteSpace(_summary) ? null : _summary,
                MarkdownBody = _body,
                ContentFormat = _format,
                Slug = string.IsNullOrWhiteSpace(_slug) ? null : _slug,
                OwnerId = _canAssignOwner ? _owner?.Id : null,
            });

            if (!response)
            {
                Snackbar.Add($"Failed to create character: {response.Error}", Severity.Error);
                return;
            }

            if (!_related.IsEmpty)
            {
                await RelatedItemsService.SetForAsync(
                    RelatedItemKind.Character,
                    response.Value.Id,
                    new SetRelatedItemsRequest
                    {
                        CharacterIds = _related.Characters.Select(r => r.Id).ToList(),
                        LogIds = _related.Logs.Select(r => r.Id).ToList(),
                        TimelineEventIds = _related.TimelineEvents.Select(r => r.Id).ToList(),
                    });
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
