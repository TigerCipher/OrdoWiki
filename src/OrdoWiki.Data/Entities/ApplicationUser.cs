using Microsoft.AspNetCore.Identity;

namespace OrdoWiki.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public bool IsPasswordResetRequired { get; set; }
}
