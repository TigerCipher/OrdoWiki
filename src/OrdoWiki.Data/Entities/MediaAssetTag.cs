namespace OrdoWiki.Data.Entities;

public class MediaAssetTag
{
    public Guid MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
