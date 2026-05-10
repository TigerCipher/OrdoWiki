namespace OrdoWiki.Web.Models;

using Data.Entities;

public sealed class GalleryFilter
{
    public MediaSourceType? SourceType { get; set; }
    public string? UploaderId { get; set; }
    public Guid? TagId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
}
