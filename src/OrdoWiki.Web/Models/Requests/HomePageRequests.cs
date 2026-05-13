namespace OrdoWiki.Web.Models.Requests;

public class UpdateBioRequest
{
    public string BioMarkdown { get; set; } = string.Empty;
}

public class SetFeaturedLogRequest
{
    public Guid? FeaturedLogId { get; set; }
}
