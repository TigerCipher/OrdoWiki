namespace OrdoWiki.Data.Entities;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public bool IsPasswordResetRequired { get; set; }

    public string? AvatarPath { get; set; }
}