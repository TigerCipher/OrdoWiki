namespace OrdoWiki.Web.Helpers;

using System.Security.Claims;
using Data.Auth;
using Models;

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

        public string? GetAvatarPath() =>
            principal.FindFirstValue(OrdoWikiClaims.AvatarPath);

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

        public string? GetPrimaryRole() =>
            principal.IsInRole(Roles.Admin) ? Roles.Admin :
            principal.IsInRole(Roles.Designer) ? Roles.Designer :
            principal.IsInRole(Roles.Editor) ? Roles.Editor :
            principal.IsInRole(Roles.Reader) ? Roles.Reader :
            null;

        public UserDto ToUserDto() =>
            new()
            {
                Id = principal.GetUserId() ?? string.Empty,
                Username = principal.GetUserName() ?? string.Empty,
                DisplayName = principal.FindFirstValue(OrdoWikiClaims.DisplayName),
                AvatarPath = principal.GetAvatarPath(),
                Role = principal.GetPrimaryRole()
            };
    }
}