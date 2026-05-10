namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrdoWiki.Web.Services;
using Shared.Dialogs;

public partial class InteractiveCarousel : IAsyncDisposable
{
    private IReadOnlyList<BannerDto> _banners = [];

    [Inject]
    private IBannerService BannerService { get; set; } = null!;

    [Inject]
    private BannerState BannerState { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        BannerState.Changed += RefreshAsync;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _banners = await BannerService.GetVisibleAsync();
        await InvokeAsync(StateHasChanged);
    }

    public ValueTask DisposeAsync()
    {
        BannerState.Changed -= RefreshAsync;
        return ValueTask.CompletedTask;
    }

    private async Task OpenFullsizeAsync(BannerDto banner)
    {
        if (string.IsNullOrEmpty(banner.ImageUrl)) return;

        DialogParameters parameters = new()
        {
            { nameof(ImageLightboxDialog.Src), banner.ImageUrl },
            { nameof(ImageLightboxDialog.Alt), banner.Alt ?? string.Empty },
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = true,
            MaxWidth = MaxWidth.False,
        };

        await DialogService.ShowAsync<ImageLightboxDialog>(string.Empty, parameters, options);
    }
}
