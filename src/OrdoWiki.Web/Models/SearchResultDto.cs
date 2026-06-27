namespace OrdoWiki.Web.Models;

public enum SearchResultKind
{
    Log,
    Character,
    TimelineEvent,
}

public class SearchResultDto
{
    public SearchResultKind Kind { get; set; }
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Href { get; set; } = string.Empty;
    public string? SnippetHtml { get; set; }
    public double Rank { get; set; }
}

public class SearchResultsDto
{
    public string Query { get; set; } = string.Empty;
    public List<SearchResultDto> Logs { get; set; } = [];
    public List<SearchResultDto> Characters { get; set; } = [];
    public List<SearchResultDto> Events { get; set; } = [];

    public int TotalCount => Logs.Count + Characters.Count + Events.Count;
}
