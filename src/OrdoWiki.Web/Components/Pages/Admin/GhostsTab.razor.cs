namespace OrdoWiki.Web.Components.Pages.Admin;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class GhostsTab
{
    private List<UserDto> _ghosts = [];
    private bool _loading = true;

    [Inject] private IGhostUserService GhostUserService { get; set; } = null!;
    [Inject] private IUserService UserService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        ApiResponse<List<UserDto>> response = await GhostUserService.ListAsync();
        _loading = false;
        if (!response.Success)
        {
            Snackbar.Add($"Failed to load ghosts: {response.Error}", Severity.Error);
            return;
        }
        _ghosts = response.Value;
    }

    private async Task OpenCreateAsync()
    {
        DialogOptions options = new() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        IDialogReference dialog = await DialogService.ShowAsync<CreateGhostDialog>("Create ghost user", options);
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false }) await ReloadAsync();
    }

    private async Task OpenLinkAsync(UserDto ghost)
    {
        DialogParameters parameters = new()
        {
            { nameof(LinkGhostDialog.Ghost), ghost },
        };
        DialogOptions options = new() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        IDialogReference dialog = await DialogService.ShowAsync<LinkGhostDialog>("Link ghost to real user", parameters, options);
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false }) await ReloadAsync();
    }

    private async Task DeleteAsync(UserDto ghost)
    {
        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Delete ghost?",
            $"Delete the ghost user \"{ghost.DisplayName ?? ghost.Username}\"? This only works if it doesn't own any content.",
            yesText: "Delete", cancelText: "Cancel");
        if (confirm != true) return;

        ApiResponse<bool> response = await GhostUserService.DeleteAsync(ghost.Id);
        if (!response.Success)
        {
            Snackbar.Add(response.Error ?? "Failed to delete", Severity.Error);
            return;
        }
        Snackbar.Add("Ghost deleted", Severity.Success);
        await ReloadAsync();
    }
}
