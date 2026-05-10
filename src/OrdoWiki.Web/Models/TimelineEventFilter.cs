namespace OrdoWiki.Web.Models;

public sealed class TimelineEventFilter
{
    /// <summary>Optional era filter. When set, results are constrained to that era's absolute year range.</summary>
    public Guid? EraId { get; set; }

    /// <summary>
    /// Optional lower year bound. Interpreted as a display year within <see cref="EraId"/> when set,
    /// or as an absolute (signed) year otherwise.
    /// </summary>
    public int? MinDisplayYear { get; set; }

    /// <summary>Optional upper year bound. Same interpretation as <see cref="MinDisplayYear"/>.</summary>
    public int? MaxDisplayYear { get; set; }

    /// <summary>Optional tag filter. When set, results include only events tagged with this tag.</summary>
    public Guid? TagId { get; set; }

    public bool Descending { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
