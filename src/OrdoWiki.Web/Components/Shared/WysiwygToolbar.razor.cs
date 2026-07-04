namespace OrdoWiki.Web.Components.Shared;

using Data.Entities;
using Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

public partial class WysiwygToolbar
{
    // Google-Docs-ish palette. A dozen picks covers most of what a wiki author
    // reaches for; a full picker is out of scope for a swatch UX.
    private static readonly string[] TextColors =
    [
        "#000000", "#5f6368", "#c0392b", "#e67e22",
        "#f1c40f", "#27ae60", "#16a085", "#2980b9",
        "#8e44ad", "#c2185b", "#795548", "#ffffff",
    ];

    private static readonly string[] HighlightColors =
    [
        "#fff59d", "#ffcc80", "#ef9a9a", "#f48fb1",
        "#ce93d8", "#9fa8da", "#90caf9", "#80deea",
        "#a5d6a7", "#e6ee9c",
    ];

    private bool _textColorOpen;
    private bool _highlightOpen;

    [Parameter, EditorRequired]
    public required string EditorId { get; set; }

    [Parameter]
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;

    [Parameter]
    public Guid? SourceId { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private async Task Exec(string command)
    {
        await JsRuntime.InvokeVoidAsync("ordoWysiwyg.exec", EditorId, command);
    }

    private async Task Exec(string command, string arg)
    {
        await JsRuntime.InvokeVoidAsync("ordoWysiwyg.exec", EditorId, command, arg);
    }

    private async Task ExecWithArg(string command, object arg)
    {
        await JsRuntime.InvokeVoidAsync("ordoWysiwyg.exec", EditorId, command, arg);
    }

    private void ToggleTextColor()
    {
        _textColorOpen = !_textColorOpen;
        _highlightOpen = false;
    }

    private void ToggleHighlight()
    {
        _highlightOpen = !_highlightOpen;
        _textColorOpen = false;
    }

    private async Task ApplyTextColor(string color)
    {
        _textColorOpen = false;
        await Exec("setColor", color);
    }

    private async Task ClearTextColor()
    {
        _textColorOpen = false;
        await Exec("unsetColor");
    }

    private async Task ApplyHighlight(string color)
    {
        _highlightOpen = false;
        await ExecWithArg("toggleHighlight", new { color });
    }

    private async Task ClearHighlight()
    {
        _highlightOpen = false;
        await Exec("unsetHighlight");
    }

    private async Task InsertLinkAsync()
    {
        string? url = await PromptAsync("Insert link", "URL", "https://");
        if (string.IsNullOrWhiteSpace(url)) return;
        await ExecWithArg("setLink", new { href = url });
    }

    private async Task InsertImageAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(UploadImageDialog.SourceType), SourceType },
            { nameof(UploadImageDialog.SourceId), SourceId },
        };

        IDialogReference dialog = await DialogService.ShowAsync<UploadImageDialog>("Insert image", parameters);
        DialogResult? result = await dialog.Result;
        if (result is not { Canceled: false, Data: ImageInsertResult inserted }) return;

        await ExecWithArg("setImage", new { src = inserted.Url, alt = inserted.AltText });
    }

    private async Task<string?> PromptAsync(string title, string label, string placeholder)
    {
        DialogParameters parameters = new()
        {
            { "Title", title },
            { "Label", label },
            { "Placeholder", placeholder },
        };

        IDialogReference dialog = await DialogService.ShowAsync<PromptDialog>(title, parameters);
        DialogResult? result = await dialog.Result;
        return result is { Canceled: false, Data: string s } ? s : null;
    }
}
