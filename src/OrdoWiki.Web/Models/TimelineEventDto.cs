namespace OrdoWiki.Web.Models;

public class TimelineEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? MarkdownBody { get; set; }
    public long EpochDayNumber { get; set; }

    public int MandoYear { get; set; }
    public int? MandoMonth { get; set; }
    public int? MandoDay { get; set; }

    public string? DisplayOverride { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public UserDto? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Server-formatted display string from the calendar service. Set on read.</summary>
    public string DisplayDate { get; set; } = string.Empty;
}
