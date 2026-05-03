namespace OrdoWiki.Web.Helpers;

using System.Security.Claims;
using Data.Auth;

public static class ClaimsExtensions
{
    extension(ClaimsPrincipal principal)
    {
        public string? GetUserId() =>
            principal.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? GetUserName() =>
            principal.Identity?.Name;

        public string? GetDisplayName() =>
            principal.FindFirstValue(OrdoWikiClaims.DisplayName) ?? principal.Identity?.Name;

        public bool IsInAnyRole(params string[] roles)
        {
            foreach (string role in roles)
            {
                if (principal.IsInRole(role)) return true;
            }

            return false;
        }

        public bool IsAdmin() => principal.IsInRole(Roles.Admin);
        public bool IsDesigner() => principal.IsInRole(Roles.Designer);
        public bool IsEditor() => principal.IsInRole(Roles.Editor);
        public bool IsReader() => principal.IsInRole(Roles.Reader);

        public bool CanEdit() =>
            principal.IsInAnyRole(Roles.Admin, Roles.Designer, Roles.Editor);

        public bool CanDesign() =>
            principal.IsInAnyRole(Roles.Admin, Roles.Designer);

        public bool MustChangePassword() =>
            principal.HasClaim(c => c.Type == OrdoWikiClaims.MustChangePassword);
    }
}