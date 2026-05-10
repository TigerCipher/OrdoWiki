namespace OrdoWiki.Web.Components.Pages.Design;

using Data.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using OrdoWiki.Web.Components.Shared.Dialogs;

public partial class BannersPage
{
    private List<BannerDto> _slots = [];
    private bool _loading = true;
    private bool _isAdmin;

    [Inject]
    private IBannerService BannerService { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthProvider { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        AuthenticationState auth = await AuthProvider.GetAuthenticationStateAsync();
        _isAdmin = auth.User.IsInRole(Roles.Admin);

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        IReadOnlyList<BannerDto> slots = await BannerService.GetSlotsAsync();
        _slots = slots.ToList();
        _loading = false;
    }

    private async Task EditAsync(BannerDto slot)
    {
        DialogParameters parameters = new()
        {
            { nameof(BannerEditDialog.Slot), slot },
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        IDialogReference dialog = await DialogService.ShowAsync<BannerEditDialog>(string.Empty, parameters, options);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: BannerDto saved })
        {
            int idx = _slots.FindIndex(s => s.SlotIndex == saved.SlotIndex);
            if (idx >= 0) _slots[idx] = saved;
            Snackbar.Add($"Slot {saved.SlotIndex} updated.", Severity.Success);
        }
    }
}
