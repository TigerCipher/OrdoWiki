namespace OrdoWiki.Web.Models;

public sealed class GalleryItemDto
{
    public required MediaAssetDto Asset { get; set; }
    public SourceLink? Source { get; set; }
    public IReadOnlyList<TagDto> Tags { get; set; } = [];
}

public sealed record SourceLink(string Kind, string Name, string Url);

public sealed class DeleteImpactDto
{
    public List<string> AffectedCharacterNames { get; set; } = [];

    public bool HasImpact => AffectedCharacterNames.Count > 0;
}
