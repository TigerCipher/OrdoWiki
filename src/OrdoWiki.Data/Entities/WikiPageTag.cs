namespace OrdoWiki.Data.Entities;

public class WikiPageTag
{
    public Guid PageId { get; set; }
    public WikiPage Page { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
