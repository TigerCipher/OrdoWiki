namespace OrdoWiki.Data.Entities;

using NpgsqlTypes;

public class WikiPage
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }

    public Guid? CurrentRevisionId { get; set; }
    public PageRevision? CurrentRevision { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser Creator { get; set; } = null!;
    public ICollection<PageRevision> Revisions { get; set; } = [];

    /// <summary>Postgres-maintained tsvector over Title + Summary (title weighted higher). Read-only.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}