namespace OrdoWiki.Web.Models.Requests;

public class CreatePageRequest
{
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public required string MarkdownBody { get; set; }
    public string? EditSummary { get; set; }
    public string? Slug { get; set; }
}

public class EditPageRequest
{
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public required string MarkdownBody { get; set; }
    public string? EditSummary { get; set; }
    public string? Slug { get; set; }
    public required Guid PageId { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}