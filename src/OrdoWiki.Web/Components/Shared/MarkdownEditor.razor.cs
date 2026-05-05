namespace OrdoWiki.Web.Components.Shared;

using Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

public partial class MarkdownEditor
{
    private const long MaxImageBytes = 10L * 1024 * 1024;

    private readonly string _editorId = $"md-editor-{Guid.NewGuid():N}";
    private readonly string _dropInputId = $"md-drop-{Guid.NewGuid():N}";
    private string _value = string.Empty;
    private string _renderedHtml = string.Empty;
    private bool _uploading;

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Label { get; set; } = "Content";

    [Parameter]
    public string Placeholder { get; set; } = "Write your markdown here...";

    [Parameter]
    public int Lines { get; set; } = 12;

    [Parameter]
    public int MaxLines { get; set; } = 30;

    [Inject]
    private IMarkdownService Markdown { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IMediaService MediaService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override void OnParametersSet()
    {
        if (_value == Value) return;

        _value = Value;
        _renderedHtml = Markdown.Render(_value);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await JsRuntime.InvokeVoidAsync("ordoEditor.attachDropZone", _editorId, _dropInputId);
    }

    private async Task OnEditorChangedAsync(string newValue)
    {
        _value = newValue;
        _renderedHtml = Markdown.Render(newValue);
        await ValueChanged.InvokeAsync(newValue);
    }

    private async Task OpenFullscreenAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(MarkdownEditorDialog.Value), _value },
            { nameof(MarkdownEditorDialog.Label), Label },
            { nameof(MarkdownEditorDialog.Placeholder), Placeholder }
        };

        DialogOptions options = new()
        {
            FullScreen = true,
            CloseButton = true,
            BackdropClick = false
        };

        IDialogReference dialog = await DialogService.ShowAsync<MarkdownEditorDialog>(Label, parameters, options);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: string updated })
        {
            _value = updated;
            _renderedHtml = Markdown.Render(updated);
            await ValueChanged.InvokeAsync(updated);
        }
    }

    private async Task OpenImageUploadAsync()
    {
        // Capture the cursor position before the dialog opens — once the textarea loses
        // focus the selection collapses and we'd insert at the wrong spot.
        CursorPosition cursor = await GetCursorAsync();

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        IDialogReference dialog = await DialogService.ShowAsync<UploadImageDialog>("Insert image", options);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: ImageInsertResult image })
        {
            await InsertAtAsync(cursor, $"![{image.AltText}]({image.Url})");
        }
    }

    private async Task OnDroppedFileAsync(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0) return;
        IBrowserFile file = e.File;

        if (file.Size > MaxImageBytes)
        {
            Snackbar.Add($"Image is larger than the {MaxImageBytes / (1024 * 1024)} MB limit.", Severity.Warning);
            return;
        }

        // The browser places the caret at the drop location, so this reads the right spot.
        CursorPosition cursor = await GetCursorAsync();

        _uploading = true;
        StateHasChanged();
        try
        {
            await using Stream stream = file.OpenReadStream(MaxImageBytes);
            ApiResponse<MediaAssetDto> response = await MediaService.UploadImageAsync(
                stream, file.Name, file.ContentType, file.Size);

            if (!response)
            {
                Snackbar.Add($"Upload failed: {response.Error}", Severity.Error);
                return;
            }

            await InsertAtAsync(cursor, $"![]({response.Value.StoragePath})");
        }
        finally
        {
            _uploading = false;
            StateHasChanged();
        }
    }

    private async Task InsertAtAsync(CursorPosition cursor, string text)
    {
        string before = _value[..Math.Min(cursor.Start, _value.Length)];
        string after = cursor.End <= _value.Length ? _value[cursor.End..] : string.Empty;
        string updated = before + text + after;

        _value = updated;
        _renderedHtml = Markdown.Render(updated);
        await ValueChanged.InvokeAsync(updated);

        StateHasChanged();
        await JsRuntime.InvokeVoidAsync("ordoEditor.setCursor", _editorId, before.Length + text.Length);
    }

    private async Task<CursorPosition> GetCursorAsync()
    {
        try
        {
            return await JsRuntime.InvokeAsync<CursorPosition>("ordoEditor.getCursor", _editorId);
        }
        catch (JSException)
        {
            return new CursorPosition(_value.Length, _value.Length);
        }
    }

    private sealed record CursorPosition(int Start, int End);
}
