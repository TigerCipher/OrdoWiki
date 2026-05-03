namespace OrdoWiki.Data.Entities;

using System.ComponentModel.DataAnnotations;

public class InviteCode
{
    public int Id { get; set; }

    [MaxLength(32)]
    public required string Code { get; set; }

    [MaxLength(32)]
    public required string AssignedRole { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedByUserId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RedeemedAt { get; set; }
    public string? RedeemedByUserId { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByUserId { get; set; }

    public bool IsRedeemed => RedeemedAt is not null;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;
    public bool IsUsable(DateTimeOffset now) => !IsRedeemed && !IsRevoked && !IsExpired(now);
}