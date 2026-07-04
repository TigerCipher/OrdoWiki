namespace OrdoWiki.Web.Services;

using System.Net;
using System.Text.RegularExpressions;
using Data.Entities;
using Ganss.Xss;

public partial class ContentRenderer : IContentRenderer
{
    [GeneratedRegex(@"<(br|hr)\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex SelfClosingBlockRegex();

    [GeneratedRegex(@"</(p|div|h[1-6]|li|blockquote|tr|pre|figure)>", RegexOptions.IgnoreCase)]
    private static partial Regex ClosingBlockRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTagRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex SpaceRunRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankRunRegex();

    private readonly IMarkdownService _markdown;
    private readonly HtmlSanitizer _htmlSanitizer;

    public ContentRenderer(IMarkdownService markdown)
    {
        _markdown = markdown;
        _htmlSanitizer = BuildEditorSanitizer();
    }

    public string Render(ContentFormat format, string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        return format switch
        {
            ContentFormat.Html => _htmlSanitizer.Sanitize(body),
            _ => _markdown.Render(body),
        };
    }

    public string SanitizeHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        return _htmlSanitizer.Sanitize(html);
    }

    public string ExtractPlainText(ContentFormat format, string? body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;
        // Markdown source is already readable — passing it through unchanged means
        // the diff shows the exact text the user typed (including `**bold**` markers),
        // which is what a wiki author expects to see line-for-line.
        if (format == ContentFormat.Markdown) return body;

        // HTML body: block boundaries become newlines so the diff has meaningful
        // lines to compare instead of one continuous run of characters. Inline
        // tags get stripped without a newline, entities get decoded, whitespace
        // collapses.
        string withBreaks = SelfClosingBlockRegex().Replace(body, "\n");
        withBreaks = ClosingBlockRegex().Replace(withBreaks, "\n");
        string stripped = AnyTagRegex().Replace(withBreaks, string.Empty);
        string decoded = WebUtility.HtmlDecode(stripped);
        string spaceCollapsed = SpaceRunRegex().Replace(decoded, " ");
        return BlankRunRegex().Replace(spaceCollapsed, "\n\n").Trim();
    }

    private static HtmlSanitizer BuildEditorSanitizer()
    {
        // The allowlist is scoped to what TipTap's StarterKit + Table + Image + Link
        // emit, plus enough style/class carry-through to keep Word/Google Docs paste
        // recognizable. Anything not on the list gets stripped — including scripts,
        // event handlers, data URIs, and unknown protocols.
        HtmlSanitizer s = new();

        s.AllowedAttributes.Add("id");
        s.AllowedAttributes.Add("class");
        s.AllowedAttributes.Add("style");
        s.AllowedAttributes.Add("colspan");
        s.AllowedAttributes.Add("rowspan");
        s.AllowedAttributes.Add("checked");
        s.AllowedAttributes.Add("disabled");
        s.AllowedAttributes.Add("type");
        s.AllowedAttributes.Add("data-type");
        s.AllowedAttributes.Add("data-checked");
        s.AllowedAttributes.Add("width");
        s.AllowedAttributes.Add("height");

        s.AllowedTags.Add("details");
        s.AllowedTags.Add("summary");
        s.AllowedTags.Add("mark");
        s.AllowedTags.Add("figure");
        s.AllowedTags.Add("figcaption");

        // Restrict style properties to visual formatting we accept from paste. Word
        // sometimes tags every span with mso-* props; those get dropped silently.
        s.AllowedCssProperties.Add("text-align");
        s.AllowedCssProperties.Add("color");
        s.AllowedCssProperties.Add("background-color");
        s.AllowedCssProperties.Add("font-weight");
        s.AllowedCssProperties.Add("font-style");
        s.AllowedCssProperties.Add("font-size");
        s.AllowedCssProperties.Add("font-family");
        s.AllowedCssProperties.Add("text-decoration");
        s.AllowedCssProperties.Add("width");
        s.AllowedCssProperties.Add("height");
        s.AllowedCssProperties.Add("max-width");

        return s;
    }
}
