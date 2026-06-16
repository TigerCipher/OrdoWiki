namespace OrdoWiki.Web.Models.Requests;

public class CreateCharacterRequest
{
    // Optional client-supplied ID so images uploaded during the create flow can be
    // attached to this character from the start (instead of being orphaned standalone).
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Summary { get; set; }
    public required string MarkdownBody { get; set; }
    public string? Slug { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}

public class EditCharacterRequest
{
    public required Guid CharacterId { get; set; }
    public required string Name { get; set; }
    public string? Summary { get; set; }
    public required string MarkdownBody { get; set; }
    public string? Slug { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
}

public class ReorderCharacterImagesRequest
{
    public required Guid CharacterId { get; set; }
    public required List<CharacterImageOrder> Order { get; set; } = [];
}

public class CharacterImageOrder
{
    public required Guid ImageId { get; set; }
    public required int OrderIndex { get; set; }
}

public class UpdateImageCaptionRequest
{
    public required Guid ImageId { get; set; }
    public string? Caption { get; set; }
}
