namespace OrdoWiki.Web.Components.Shared;

using Data.Entities;
using Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

public partial class MarkdownToolbar
{
    private bool _uploading;

    [Parameter, EditorRequired]
    public string EditorId { get; set; } = string.Empty;

    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;

    [Parameter]
    public Guid? SourceId { get; set; }

    [Parameter]
    public bool ShowFullscreen { get; set; }

    [Parameter]
    public EventCallback OnFullscreen { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private static Dictionary<string, object?> Attrs(string label)
        => new() { { "aria-label", label }, { "title", label } };

    private Task BoldAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.wrapSelection", EditorId, "**", "**", "bold text").AsTask();

    private Task ItalicAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.wrapSelection", EditorId, "*", "*", "italic text").AsTask();

    private Task StrikethroughAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.wrapSelection", EditorId, "~~", "~~", "strikethrough").AsTask();

    private Task InlineCodeAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.wrapSelection", EditorId, "`", "`", "code").AsTask();

    private Task BulletListAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.prefixLines", EditorId, "- ", false).AsTask();

    private Task NumberedListAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.prefixLines", EditorId, string.Empty, true).AsTask();

    private Task QuoteAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.prefixLines", EditorId, "> ", false).AsTask();

    private Task SetHeadingAsync(int level)
    {
        string prefix = new string('#', level) + " ";
        return JsRuntime.InvokeVoidAsync("ordoEditor.setHeading", EditorId, prefix).AsTask();
    }

    private Task CodeBlockAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.insertBlock", EditorId, "```\ncode\n```").AsTask();

    private Task TableAsync()
    {
        const string table = "| Column 1 | Column 2 | Column 3 |\n| -------- | -------- | -------- |\n| Cell     | Cell     | Cell     |\n| Cell     | Cell     | Cell     |";
        return JsRuntime.InvokeVoidAsync("ordoEditor.insertBlock", EditorId, table).AsTask();
    }

    private Task HorizontalRuleAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.insertBlock", EditorId, "---").AsTask();

    private Task ParagraphAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.insertBlock", EditorId, string.Empty).AsTask();

    // CommonMark collapses leading whitespace inside paragraphs, so a real
    // first-line indent has to be an em-space entity that survives Markdig
    // and the HTML sanitizer. Clicking again adds another level.
    private Task IndentParagraphAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.prefixLines", EditorId, "&emsp;", false).AsTask();

    private Task LinkAsync()
        => JsRuntime.InvokeVoidAsync("ordoEditor.insertLink", EditorId, "link text", "https://").AsTask();

    private async Task OpenImageUploadAsync()
    {
        // Capture the cursor position before the dialog opens — once the textarea
        // loses focus the selection collapses and the insert lands at index 0.
        CursorPosition cursor = await GetCursorAsync();

        DialogOptions options = new()
        {
            CloseButton = true,
            BackdropClick = false,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        DialogParameters parameters = new()
        {
            { nameof(UploadImageDialog.SourceType), SourceType },
            { nameof(UploadImageDialog.SourceId), SourceId }
        };

        IDialogReference dialog = await DialogService.ShowAsync<UploadImageDialog>("Insert image", parameters, options);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false, Data: ImageInsertResult image })
        {
            await JsRuntime.InvokeVoidAsync(
                "ordoEditor.insertAtRange",
                EditorId,
                cursor.Start,
                cursor.End,
                $"![{image.AltText}]({image.Url})");
        }
    }

    private async Task<CursorPosition> GetCursorAsync()
    {
        try
        {
            return await JsRuntime.InvokeAsync<CursorPosition>("ordoEditor.getCursor", EditorId);
        }
        catch (JSException)
        {
            return new CursorPosition(0, 0);
        }
    }

    public void SetUploading(bool uploading)
    {
        _uploading = uploading;
        StateHasChanged();
    }

    private sealed record CursorPosition(int Start, int End);
}
