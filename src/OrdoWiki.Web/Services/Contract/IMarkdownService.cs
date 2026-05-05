namespace OrdoWiki.Web.Services.Contract;

public interface IMarkdownService
{
    string Render(string? markdown);
}
