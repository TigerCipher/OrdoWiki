namespace OrdoWiki.Web.Models.Requests;

public sealed class SetRelatedItemsRequest
{
    public IReadOnlyList<Guid> CharacterIds { get; set; } = [];
    public IReadOnlyList<Guid> LogIds { get; set; } = [];
    public IReadOnlyList<Guid> TimelineEventIds { get; set; } = [];
}
