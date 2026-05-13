namespace OrdoWiki.Web.Models;

public class HomePageDto
{
    public string BioMarkdown { get; set; } = string.Empty;
    public string BioHtml { get; set; } = string.Empty;

    public Guid? FeaturedLogId { get; set; }
    public WikiPageDto? FeaturedLog { get; set; }

    public List<TimelineEventDto> RecentEvents { get; set; } = [];
}
