namespace OrdoWiki.Data.Entities;

// Polymorphic relation row. To make A<->B symmetric without storing two rows,
// rows are canonicalized so the (Kind, Id) tuple ordered smaller is always the
// "source" side. Reads union both sides for a given entity.
public class RelatedItem
{
    public Guid Id { get; set; }

    public RelatedItemKind SourceKind { get; set; }
    public Guid SourceId { get; set; }

    public RelatedItemKind TargetKind { get; set; }
    public Guid TargetId { get; set; }

    public DateTime CreatedAt { get; set; }
}
