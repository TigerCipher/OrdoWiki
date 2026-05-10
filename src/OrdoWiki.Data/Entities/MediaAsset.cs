namespace OrdoWiki.Data.Entities;

public class MediaAsset
{
    public Guid Id { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser? UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;
    public Guid? SourceId { get; set; }
}
