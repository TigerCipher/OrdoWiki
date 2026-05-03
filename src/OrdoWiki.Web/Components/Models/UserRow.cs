namespace OrdoWiki.Web.Components.Models;

public record UserRow(
    string Id,
    string Username,
    string DisplayName,
    IList<string> Roles,
    bool IsLocked,
    bool MustResetPassword);
