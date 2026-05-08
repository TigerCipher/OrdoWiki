namespace OrdoWiki.Web.Models;

public class CharacterImageDto
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid MediaAssetId { get; set; }
    public string? Caption { get; set; }
    public int OrderIndex { get; set; }
    public MediaAssetDto? MediaAsset { get; set; }
}
