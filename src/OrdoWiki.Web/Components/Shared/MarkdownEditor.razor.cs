namespace OrdoWiki.Web.Components.Shared;

using Data.Entities;
using Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OrdoWiki.Web.Services;

public partial class MarkdownEditor
{
    private readonly string _editorId = $"md-editor-{Guid.NewGuid():N}";
    private readonly string _dropInputId = $"md-drop-{Guid.NewGuid():N}";
    private string _value = string.Empty;
    private string _renderedHtml = string.Empty;
    private MarkdownToolbar? _toolbar;

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

    [Parameter]
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;

    [Parameter]
    public Guid? SourceId { get; set; }

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
            { nameof(MarkdownEditorDialog.Placeholder), Placeholder },
            { nameof(MarkdownEditorDialog.SourceType), SourceType },
            { nameof(MarkdownEditorDialog.SourceId), SourceId }
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

    private async Task OnDroppedFileAsync(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0) return;
        IBrowserFile file = e.File;

        if (file.Size > MediaLimits.MaxImageBytes)
        {
            Snackbar.Add($"Image is larger than the {MediaLimits.MaxImageBytes / (1024 * 1024)} MB limit.", Severity.Warning);
            return;
        }

        // The browser places the caret at the drop location, so this reads the right spot.
        CursorPosition cursor = await GetCursorAsync();

        _toolbar?.SetUploading(true);
        try
        {
            await using Stream stream = file.OpenReadStream(MediaLimits.MaxImageBytes);
            ApiResponse<MediaAssetDto> response = await MediaService.UploadImageAsync(
                stream, file.Name, file.ContentType, file.Size, SourceType, SourceId);

            if (!response)
            {
                Snackbar.Add($"Upload failed: {response.Error}", Severity.Error);
                return;
            }

            await JsRuntime.InvokeVoidAsync(
                "ordoEditor.insertAtRange",
                _editorId,
                cursor.Start,
                cursor.End,
                $"![]({response.Value.StoragePath})");
        }
        finally
        {
            _toolbar?.SetUploading(false);
        }
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
