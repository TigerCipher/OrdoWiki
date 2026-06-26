namespace OrdoWiki.Web.Models.Requests;

public class CreateGhostUserRequest
{
    public required string DisplayName { get; set; }
}

public class LinkGhostUserRequest
{
    public required string GhostId { get; set; }
    public required string RealUserId { get; set; }
}
