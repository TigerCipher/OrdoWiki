namespace OrdoWiki.Data.Entities;

using NpgsqlTypes;

public class TimelineEvent
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? MarkdownBody { get; set; }

    /// <summary>Canonical sort key — derived from <c>MandoYear/Month/Day</c> on save.</summary>
    public long EpochDayNumber { get; set; }

    /// <summary>Signed absolute year. Era + display year are derived from this at render time.</summary>
    public int MandoYear { get; set; }
    public int? MandoMonth { get; set; }
    public int? MandoDay { get; set; }

    /// <summary>Optional override that replaces the auto-formatted date string.</summary>
    public string? DisplayOverride { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Postgres-maintained tsvector over Title + Summary + MarkdownBody. Read-only.</summary>
    public NpgsqlTsVector SearchVector { get; set; } = null!;
}
