namespace OrdoWiki.Web.Models;

public class PageRevisionDto
{
    public Guid Id { get; set; }
    public string MarkdownBody { get; set; } = string.Empty;
    public string? EditSummary { get; set; }
    public DateTime EditedAt { get; set; }
    public string EditedById { get; set; } = string.Empty;
    public UserDto? Editor { get; set; }
    public Guid PageId { get; set; }
    public WikiPageDto? Page { get; set; }
}