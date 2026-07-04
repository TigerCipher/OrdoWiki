namespace OrdoWiki.Web.Services;

using Data.Entities;
using Ganss.Xss;

public class ContentRenderer : IContentRenderer
{
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
        s.AllowedCssProperties.Add("text-decoration");
        s.AllowedCssProperties.Add("width");

        return s;
    }
}
