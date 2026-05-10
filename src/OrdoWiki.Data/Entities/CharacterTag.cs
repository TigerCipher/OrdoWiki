namespace OrdoWiki.Data.Entities;

public class CharacterTag
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
