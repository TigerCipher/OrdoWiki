namespace OrdoWiki.Web.Components.Shared;

using Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class MarkdownEditor
{
    private string _value = string.Empty;
    private string _renderedHtml = string.Empty;

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

    protected override void OnParametersSet()
    {
        if (_value == Value) return;

        _value = Value;
        _renderedHtml = Markdown.Render(_value);
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
}
