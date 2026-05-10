namespace OrdoWiki.Web.Models;

public class CharacterDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string MarkdownBody { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;
    public UserDto? Owner { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<CharacterImageDto> Images { get; set; } = [];

    public IReadOnlyList<TagDto> Tags { get; set; } = [];
}
