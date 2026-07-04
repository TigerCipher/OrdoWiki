namespace OrdoWiki.Web.Components.Shared;

using Data.Entities;
using Microsoft.AspNetCore.Components;

public partial class RichContentEditor
{
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public ContentFormat Format { get; set; } = ContentFormat.Html;

    [Parameter]
    public EventCallback<ContentFormat> FormatChanged { get; set; }

    [Parameter]
    public string Label { get; set; } = "Content";

    [Parameter]
    public string Placeholder { get; set; } = "Write your content here...";

    [Parameter]
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;

    [Parameter]
    public Guid? SourceId { get; set; }

    /// <summary>Show the toggle only where format changes are safe (create pages).
    /// Edit pages leave it off so existing content isn't accidentally cross-converted.</summary>
    [Parameter]
    public bool ShowFormatToggle { get; set; } = true;

    private async Task OnValueChangedAsync(string newValue)
    {
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
    }

    private async Task OnFormatChangedAsync(ContentFormat newFormat)
    {
        if (newFormat == Format) return;

        // Switching editors on the fly would silently drop content — the two editors
        // work with different string shapes and no round-trip conversion is lossless.
        // Reset the body so the user starts fresh in the new editor. The toggle only
        // ships on create pages, so this can only affect draft content they haven't
        // saved yet.
        Format = newFormat;
        Value = string.Empty;
        await FormatChanged.InvokeAsync(newFormat);
        await ValueChanged.InvokeAsync(string.Empty);
    }
}
