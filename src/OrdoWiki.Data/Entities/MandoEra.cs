namespace OrdoWiki.Data.Entities;

using Calendars;

public class MandoEra
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// Signed absolute year that anchors the era. Forward eras start counting from
    /// (and including) this year; backward eras count down toward it.
    /// </summary>
    public int AnchorYear { get; set; }

    public EraDirection Direction { get; set; }

    /// <summary>For UI ordering only — chronology is inferred from AnchorYear + Direction.</summary>
    public int SortOrder { get; set; }
}
