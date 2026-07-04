namespace OrdoWiki.Data.Entities;

using NpgsqlTypes;

public class Character
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string MarkdownBody { get; set; } = string.Empty;
    public ContentFormat ContentFormat { get; set; } = ContentFormat.Markdown;

    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CharacterImage> Images { get; set; } = [];

    /// <summary>Postgres-maintained tsvector over Name + Summary + MarkdownBody. Read-only.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
