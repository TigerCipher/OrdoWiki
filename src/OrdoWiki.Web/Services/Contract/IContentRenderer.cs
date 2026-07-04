namespace OrdoWiki.Web.Services.Contract;

using Data.Entities;

public interface IContentRenderer
{
    /// <summary>Renders a body to sanitized HTML for viewer pages, dispatching by format.</summary>
    string Render(ContentFormat format, string? body);

    /// <summary>Sanitizes untrusted HTML before persistence. Use in the write path
    /// so we never trust the client alone.</summary>
    string SanitizeHtml(string? html);
}
