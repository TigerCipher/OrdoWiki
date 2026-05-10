namespace OrdoWiki.Web.Models;

using Data.Calendars;

public class MandoEraDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int AnchorYear { get; set; }
    public EraDirection Direction { get; set; }
    public int SortOrder { get; set; }

    public MandoEraInfo ToInfo() => new(Name, ShortCode, AnchorYear, Direction);
}
