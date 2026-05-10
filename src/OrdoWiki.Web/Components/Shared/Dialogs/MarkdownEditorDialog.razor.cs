namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OrdoWiki.Web.Services;

public partial class MarkdownEditorDialog
{
    private readonly string _editorId = $"md-editor-dlg-{Guid.NewGuid():N}";
    private readonly string _dropInputId = $"md-drop-dlg-{Guid.NewGuid():N}";
    private string _value = string.Empty;
    private string _renderedHtml = string.Empty;
    private bool _uploading;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public string Label { get; set; } = "Content";

    [Parameter]
    public string Placeholder { get; set; } = "Write your markdown here...";

    [Parameter]
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;

    [Parameter]
    public Guid? SourceId { get; set; }

    [Inject]
    private IMarkdownService Markdown { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IMediaService MediaService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override void OnInitialized()
    {
        _value = Value;
        _renderedHtml = Markdown.Render(_value);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await JsRuntime.InvokeVoidAsync("ordoEditor.attachDropZone", _editorId, _dropInputId);
    }

    private void OnEditorChanged(string newValue)
    {
        _value = newValue;
        _renderedHtml = Markdown.Render(newValue);
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

        _uploading = true;
        StateHasChanged();
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

    private void Apply() => MudDialog.Close(DialogResult.Ok(_value));

    private void Cancel() => MudDialog.Cancel();

    private sealed record CursorPosition(int Start, int End);
}
