namespace OrdoWiki.Data.Entities;

public class CharacterImage
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;

    public Guid MediaAssetId { get; set; }
    public MediaAsset MediaAsset { get; set; } = null!;

    public string? Caption { get; set; }
    public int OrderIndex { get; set; }
}
