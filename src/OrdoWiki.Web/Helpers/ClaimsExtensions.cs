using System.Security.Claims;
using OrdoWiki.Data.Auth;

namespace OrdoWiki.Web.Helpers;

public static class ClaimsExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetUserName(this ClaimsPrincipal principal) =>
        principal.Identity?.Name;

    public static string? GetDisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(OrdoWikiClaims.DisplayName) ?? principal.Identity?.Name;

    public static bool IsInAnyRole(this ClaimsPrincipal principal, params string[] roles)
    {
        foreach (string role in roles)
        {
            if (principal.IsInRole(role))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(Roles.Admin);
    public static bool IsDesigner(this ClaimsPrincipal principal) => principal.IsInRole(Roles.Designer);
    public static bool IsEditor(this ClaimsPrincipal principal) => principal.IsInRole(Roles.Editor);
    public static bool IsReader(this ClaimsPrincipal principal) => principal.IsInRole(Roles.Reader);

    public static bool CanEdit(this ClaimsPrincipal principal) =>
        principal.IsInAnyRole(Roles.Admin, Roles.Designer, Roles.Editor);

    public static bool CanDesign(this ClaimsPrincipal principal) =>
        principal.IsInAnyRole(Roles.Admin, Roles.Designer);

    public static bool MustChangePassword(this ClaimsPrincipal principal) =>
        principal.HasClaim(c => c.Type == OrdoWikiClaims.MustChangePassword);
}
