namespace OrdoWiki.Data.Entities;

public class Banner
{
    public Guid Id { get; set; }

    /// <summary>1-3 are designer-editable, 4 is admin-reserved.</summary>
    public int SlotIndex { get; set; }

    public Guid? MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }

    public string? Alt { get; set; }

    /// <summary>Optional click-through URL. When null the banner opens a lightbox instead.</summary>
    public string? LinkUrl { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }
}
