namespace OrdoWiki.Web.Models.Requests;

using Data.Calendars;

public sealed class RenameMonthRequest
{
    public int MonthIndex { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CreateEraRequest
{
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int AnchorYear { get; set; }
    public EraDirection Direction { get; set; }
    public int SortOrder { get; set; }
}

public sealed class UpdateEraRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public int AnchorYear { get; set; }
    public EraDirection Direction { get; set; }
    public int SortOrder { get; set; }
}
