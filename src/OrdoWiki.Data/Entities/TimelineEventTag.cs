namespace OrdoWiki.Data.Entities;

public class TimelineEventTag
{
    public Guid TimelineEventId { get; set; }
    public TimelineEvent TimelineEvent { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
