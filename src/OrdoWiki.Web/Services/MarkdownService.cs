namespace OrdoWiki.Web.Services;

using System.Text.RegularExpressions;
using Ganss.Xss;
using Markdig;

public partial class MarkdownService : IMarkdownService
{
    // Markdig emits footnote anchors as `id="fn:N"` / `href="#fn:N"`. The colon
    // breaks CSS selectors (`:N` parses as a pseudo-class) which trips Blazor's
    // enhanced-nav click handler and sends the user to "/" instead of scrolling.
    // Rewrite to dash-style ids before sanitizing.
    [GeneratedRegex(@"\b(id|href)=""(#?)(fn(?:ref)?):(\d+)""", RegexOptions.IgnoreCase)]
    private static partial Regex FootnoteAnchorRegex();

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()            // footnotes, task lists, definition lists, tables, sub/sup, custom containers, etc.
        .UseAlertBlocks()                   // GitHub-style `> [!NOTE]` callouts.
        .UseEmojiAndSmiley()                // :smile: -> emoji.
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    private readonly HtmlSanitizer _sanitizer;

    public MarkdownService()
    {
        _sanitizer = new HtmlSanitizer();

        // Footnote anchors need `id` to scroll; alert/task-list markup needs class and
        // the checkbox attributes. These are added defensively — duplicates are fine.
        _sanitizer.AllowedAttributes.Add("id");
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("type");
        _sanitizer.AllowedAttributes.Add("checked");
        _sanitizer.AllowedAttributes.Add("disabled");

        // Collapsible disclosure widgets — <details>/<summary> are safe and widely used.
        _sanitizer.AllowedTags.Add("details");
        _sanitizer.AllowedTags.Add("summary");
    }

    public string Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        string html = Markdown.ToHtml(markdown, _pipeline);
        html = FootnoteAnchorRegex().Replace(html, "$1=\"$2$3-$4\"");
        return _sanitizer.Sanitize(html);
    }
}
