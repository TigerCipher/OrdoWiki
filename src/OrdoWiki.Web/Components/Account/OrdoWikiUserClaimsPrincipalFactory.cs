namespace OrdoWiki.Web.Components.Account;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OrdoWiki.Data.Auth;
using OrdoWiki.Data.Entities;


internal sealed class OrdoWikiUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);

        if (user.IsPasswordResetRequired)
        {
            identity.AddClaim(new Claim(OrdoWikiClaims.MustChangePassword, "true"));
        }

        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            identity.AddClaim(new Claim(OrdoWikiClaims.DisplayName, user.DisplayName));
        }

        return identity;
    }
}
