namespace OrdoWiki.Data.Entities;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public bool IsPasswordResetRequired { get; set; }

    public string? AvatarPath { get; set; }

    // Placeholder accounts created by admins for friends who haven't signed up
    // yet. They own content but cannot sign in. When the friend joins, an admin
    // links the ghost to their real account and the ghost is deleted.
    public bool IsGhost { get; set; }
}