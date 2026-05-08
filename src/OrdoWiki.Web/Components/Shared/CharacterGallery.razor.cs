namespace OrdoWiki.Web.Components.Shared;

using Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class CharacterGallery
{
    [Parameter, EditorRequired]
    public required List<CharacterImageDto> Images { get; set; }

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private async Task OpenLightboxAsync(CharacterImageDto image)
    {
        if (image.MediaAsset is null) return;

        DialogParameters parameters = new()
        {
            { nameof(ImageLightboxDialog.Src), image.MediaAsset.StoragePath },
            { nameof(ImageLightboxDialog.Alt), image.Caption ?? string.Empty },
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
