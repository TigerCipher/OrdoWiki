namespace OrdoWiki.Web.Components.Models;

public enum InviteStatus
{
    Active,
    Redeemed,
    Revoked,
    Expired
}

public record InviteRow(
    int Id,
    string Code,
    string Role,
    InviteStatus Status,
    string CreatedBy,
    DateTimeOffset ExpiresAt);