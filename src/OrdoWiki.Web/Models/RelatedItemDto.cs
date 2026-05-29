namespace OrdoWiki.Web.Models;

using OrdoWiki.Data.Entities;

public class RelatedItemRef
{
    public Guid Id { get; set; }
    public RelatedItemKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
}

public class RelatedItemsDto
{
    public List<RelatedItemRef> Characters { get; set; } = [];
    public List<RelatedItemRef> Logs { get; set; } = [];
    public List<RelatedItemRef> TimelineEvents { get; set; } = [];

    public bool IsEmpty => Characters.Count == 0 && Logs.Count == 0 && TimelineEvents.Count == 0;
}
