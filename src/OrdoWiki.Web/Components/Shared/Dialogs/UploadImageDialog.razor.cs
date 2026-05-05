namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

public partial class UploadImageDialog
{
    private const long MaxImageBytes = 10L * 1024 * 1024;

    private readonly string _inputId = $"upload-{Guid.NewGuid():N}";
    private IBrowserFile? _file;
    private string _altText = string.Empty;
    private string? _error;
    private bool _uploading;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private IMediaService MediaService { get; set; } = null!;

    private void OnFilePicked(InputFileChangeEventArgs e)
    {
        _error = null;
        IBrowserFile? picked = e.FileCount > 0 ? e.File : null;
        if (picked is null)
        {
            _file = null;
            return;
        }

        if (picked.Size > MaxImageBytes)
        {
            _error = $"Image is {FormatBytes(picked.Size)} — max is {FormatBytes(MaxImageBytes)}.";
            _file = null;
            return;
        }

        _file = picked;
    }

    private async Task UploadAsync()
    {
        if (_file is null) return;

        _error = null;
        _uploading = true;
        try
        {
            await using Stream stream = _file.OpenReadStream(MaxImageBytes);
            ApiResponse<MediaAssetDto> response = await MediaService.UploadImageAsync(
                stream, _file.Name, _file.ContentType, _file.Size);

            if (!response)
            {
                _error = response.Error;
                return;
            }

            ImageInsertResult result = new(response.Value.StoragePath, _altText.Trim());
            MudDialog.Close(DialogResult.Ok(result));
        }
        finally
        {
            _uploading = false;
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

public sealed record ImageInsertResult(string Url, string AltText);
