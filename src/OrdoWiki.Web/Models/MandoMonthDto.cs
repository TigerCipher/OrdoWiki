namespace OrdoWiki.Web.Models;

public class MandoMonthDto
{
    public Guid Id { get; set; }
    public int MonthIndex { get; set; }
    public string Name { get; set; } = string.Empty;
}
