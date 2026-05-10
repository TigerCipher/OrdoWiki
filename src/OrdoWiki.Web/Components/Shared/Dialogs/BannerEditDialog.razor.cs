namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OrdoWiki.Web.Models.Requests;
using OrdoWiki.Web.Services;

public partial class BannerEditDialog
{
    private readonly string _inputId = $"banner-upload-{Guid.NewGuid():N}";
    private Guid? _mediaAssetId;
    private string? _imageUrl;
    private string _altText = string.Empty;
    private string _linkUrl = string.Empty;
    private string? _error;
    private bool _uploading;
    private bool _saving;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required BannerDto Slot { get; set; }

    [Inject]
    private IMediaService MediaService { get; set; } = null!;

    [Inject]
    private IBannerService BannerService { get; set; } = null!;

    protected override void OnInitialized()
    {
        _mediaAssetId = Slot.MediaAssetId;
        _imageUrl = Slot.ImageUrl;
        _altText = Slot.Alt ?? string.Empty;
        _linkUrl = Slot.LinkUrl ?? string.Empty;
    }

    private async Task OnFilePickedAsync(InputFileChangeEventArgs e)
    {
        _error = null;
        if (e.FileCount == 0) return;

        IBrowserFile picked = e.File;
        if (picked.Size > MediaLimits.MaxImageBytes)
        {
            _error = $"Image is {FormatBytes(picked.Size)} — max is {FormatBytes(MediaLimits.MaxImageBytes)}.";
            return;
        }

        _uploading = true;
        try
        {
            await using Stream stream = picked.OpenReadStream(MediaLimits.MaxImageBytes);
            ApiResponse<MediaAssetDto> response = await MediaService.UploadImageAsync(
                stream, picked.Name, picked.ContentType, picked.Size,
                MediaSourceType.Banner, Slot.Id);

            if (!response)
            {
                _error = response.Error;
                return;
            }

            _mediaAssetId = response.Value.Id;
            _imageUrl = response.Value.StoragePath;
        }
        finally
        {
            _uploading = false;
        }
    }

    private void ClearImage()
    {
        _mediaAssetId = null;
        _imageUrl = null;
    }

    private async Task SaveAsync()
    {
        _error = null;
        _saving = true;
        try
        {
            ApiResponse<BannerDto> response = await BannerService.SetAsync(new SetBannerRequest
            {
                SlotIndex = Slot.SlotIndex,
                MediaAssetId = _mediaAssetId,
                Alt = _altText,
                LinkUrl = _linkUrl,
            });

            if (!response.Success)
            {
                _error = response.Error;
                return;
            }

            MudDialog.Close(DialogResult.Ok(response.Value));
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.##} MB",
    };
}
