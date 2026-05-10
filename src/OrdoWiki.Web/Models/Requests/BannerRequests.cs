namespace OrdoWiki.Web.Models.Requests;

public sealed class SetBannerRequest
{
    public required int SlotIndex { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? Alt { get; set; }
    public string? LinkUrl { get; set; }
}
