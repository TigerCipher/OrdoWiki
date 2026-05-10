namespace OrdoWiki.Web.Models;

public class TagDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? UsageCount { get; set; }

    public override string ToString() => Name;
}

public enum TagTarget
{
    WikiPage = 0,
    Character = 1,
    MediaAsset = 2,
    TimelineEvent = 3,
}
