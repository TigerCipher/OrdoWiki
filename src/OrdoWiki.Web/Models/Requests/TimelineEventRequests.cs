namespace OrdoWiki.Web.Models.Requests;

using Data.Entities;

public sealed class CreateTimelineEventRequest
{
    // Optional client-supplied ID so images uploaded during the create flow can be
    // attached to this event from the start (instead of being orphaned standalone).
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? MarkdownBody { get; set; }
    public ContentFormat ContentFormat { get; set; } = ContentFormat.Markdown;
    public int MandoYear { get; set; }
    public int? MandoMonth { get; set; }
    public int? MandoDay { get; set; }
    public string? DisplayOverride { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}

public sealed class UpdateTimelineEventRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? MarkdownBody { get; set; }
    public ContentFormat ContentFormat { get; set; } = ContentFormat.Markdown;
    public int MandoYear { get; set; }
    public int? MandoMonth { get; set; }
    public int? MandoDay { get; set; }
    public string? DisplayOverride { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}
