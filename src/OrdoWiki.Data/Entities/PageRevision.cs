namespace OrdoWiki.Data.Entities;

using NpgsqlTypes;

public class PageRevision
{
    public Guid Id { get; set; }
    public string MarkdownBody { get; set; } = string.Empty;
    public ContentFormat ContentFormat { get; set; } = ContentFormat.Markdown;
    public string? EditSummary { get; set; }
    public DateTime EditedAt { get; set; }
    public string EditedById { get; set; } = string.Empty;
    public ApplicationUser Editor { get; set; } = null!;
    public Guid PageId { get; set; }
    public WikiPage Page { get; set; } = null!;

    /// <summary>Postgres-maintained tsvector over MarkdownBody. Read-only.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}