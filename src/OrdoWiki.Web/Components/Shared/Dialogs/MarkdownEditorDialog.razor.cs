namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class MarkdownEditorDialog
{
    private string _value = string.Empty;
    private string _renderedHtml = string.Empty;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public string Label { get; set; } = "Content";

    [Parameter]
    public string Placeholder { get; set; } = "Write your markdown here...";

    [Inject]
    private IMarkdownService Markdown { get; set; } = null!;

    protected override void OnInitialized()
    {
        _value = Value;
        _renderedHtml = Markdown.Render(_value);
    }

    private void OnEditorChanged(string newValue)
    {
        _value = newValue;
        _renderedHtml = Markdown.Render(newValue);
    }

    private void Apply() => MudDialog.Close(DialogResult.Ok(_value));

    private void Cancel() => MudDialog.Cancel();
}
