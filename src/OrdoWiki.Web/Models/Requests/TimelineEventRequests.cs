namespace OrdoWiki.Web.Models.Requests;

public sealed class CreateTimelineEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? MarkdownBody { get; set; }
    public int MandoYear { get; set; }
    public int? MandoMonth { get; set; }
    public int? MandoDay { get; set; }
    public string? DisplayOverride { get; set; }
}

public sealed class UpdateTimelineEventRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? MarkdownBody { get; set; }
    public int MandoYear { get; set; }
    public int? MandoMonth { get; set; }
    public int? MandoDay { get; set; }
    public string? DisplayOverride { get; set; }
}
