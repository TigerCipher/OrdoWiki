namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;

public partial class SeeAlsoSection
{
    [Parameter, EditorRequired]
    public RelatedItemsDto Value { get; set; } = new();
}
