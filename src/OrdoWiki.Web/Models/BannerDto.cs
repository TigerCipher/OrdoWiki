namespace OrdoWiki.Web.Models;

public sealed class BannerDto
{
    public Guid Id { get; set; }
    public int SlotIndex { get; set; }

    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }

    public string? Alt { get; set; }
    public string? LinkUrl { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>True for slot 4 — only Admins can edit it.</summary>
    public bool AdminOnly => SlotIndex == 4;
}
