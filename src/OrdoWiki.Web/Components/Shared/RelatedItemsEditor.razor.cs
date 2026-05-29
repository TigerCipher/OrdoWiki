namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using OrdoWiki.Data.Entities;

public partial class RelatedItemsEditor
{
    [Parameter, EditorRequired]
    public RelatedItemsDto Value { get; set; } = new();

    [Parameter]
    public EventCallback<RelatedItemsDto> ValueChanged { get; set; }

    [Parameter]
    public RelatedItemKind? SelfKind { get; set; }

    [Parameter]
    public Guid? SelfId { get; set; }

    private Task OnChanged(
        IReadOnlyList<RelatedItemRef> characters,
        IReadOnlyList<RelatedItemRef> logs,
        IReadOnlyList<RelatedItemRef> events)
    {
        RelatedItemsDto next = new()
        {
            Characters = characters.ToList(),
            Logs = logs.ToList(),
            TimelineEvents = events.ToList(),
        };
        return ValueChanged.InvokeAsync(next);
    }
}
