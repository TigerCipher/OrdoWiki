namespace OrdoWiki.Data.Entities;

public class MandoMonth
{
    public Guid Id { get; set; }

    /// <summary>1-based month-of-year, 1..12.</summary>
    public int MonthIndex { get; set; }

    public string Name { get; set; } = string.Empty;
}
