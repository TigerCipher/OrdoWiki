namespace OrdoWiki.Web.Models;

public class WikiPageDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public string? Summary { get; set; }
    
    public Guid? CurrentRevisionId { get; set; }
    public PageRevisionDto? CurrentRevision { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string CreatedById { get; set; } = string.Empty;
    public UserDto? Creator { get; set; }
    public List<PageRevisionDto> Revisions { get; set; } = [];

    public IReadOnlyList<TagDto> Tags { get; set; } = [];

    public string CreatorName => Creator?.DisplayName ?? "<unknown>";
}