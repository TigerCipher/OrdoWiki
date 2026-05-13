namespace OrdoWiki.Data.Entities;

/// <summary>Singleton row holding the homepage bio + admin-selected featured log.</summary>
public class HomePage
{
    public static readonly Guid SingletonId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }

    public string BioMarkdown { get; set; } = string.Empty;

    public Guid? FeaturedLogId { get; set; }
    public WikiPage? FeaturedLog { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }
}
