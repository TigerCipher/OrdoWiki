namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Dialogs;

public partial class InteractiveCarousel
{
    private static readonly BannerImage[] _banners =
    [
        new("/img/Thrac_and_Vhosa_zoomed.jpg", "Thrac & Vhosa staring longingly at each other", Color.Tertiary),
        new("/img/After_Party.jpg", "The After Party - Tana's Masterpiece", Color.Dark),
    ];

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private async Task OpenFullsizeAsync(BannerImage banner)
    {
        DialogParameters parameters = new()
        {
            { nameof(ImageLightboxDialog.Src), banner.Src },
            { nameof(ImageLightboxDialog.Alt), banner.Alt },
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = true,
            MaxWidth = MaxWidth.False,
        };

        await DialogService.ShowAsync<ImageLightboxDialog>(string.Empty, parameters, options);
    }

    private sealed record BannerImage(string Src, string Alt, Color Color);
}
