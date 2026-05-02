using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OrdoWiki.Data.Entities;

namespace OrdoWiki.Data.Auth;

public class InviteCodeService(ApplicationDbContext db, TimeProvider timeProvider)
{
    private const int CodeBytes = 9; // 9 bytes -> 12 base64url chars
    public const int DefaultExpirationDays = 7;

    public async Task<InviteCode> GenerateAsync(
        string assignedRole,
        string createdByUserId,
        int expiresInDays = DefaultExpirationDays,
        CancellationToken ct = default)
    {
        if (!Roles.All.Contains(assignedRole))
        {
            throw new ArgumentException($"Unknown role '{assignedRole}'.", nameof(assignedRole));
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        InviteCode invite = new()
        {
            Code = GenerateCode(),
            AssignedRole = assignedRole,
            CreatedAt = now,
            CreatedByUserId = createdByUserId,
            ExpiresAt = now.AddDays(expiresInDays),
        };

        db.InviteCodes.Add(invite);
        await db.SaveChangesAsync(ct);
        return invite;
    }

    public async Task<InviteCode?> FindUsableAsync(string code, CancellationToken ct = default)
    {
        InviteCode? invite = await db.InviteCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Code == code, ct);

        return invite is not null && invite.IsUsable(timeProvider.GetUtcNow())
            ? invite
            : null;
    }

    public async Task<InviteCode?> RedeemAsync(string code, string redeemedByUserId, CancellationToken ct = default)
    {
        InviteCode? invite = await db.InviteCodes
            .SingleOrDefaultAsync(c => c.Code == code, ct);

        if (invite is null || !invite.IsUsable(timeProvider.GetUtcNow()))
        {
            return null;
        }

        invite.RedeemedAt = timeProvider.GetUtcNow();
        invite.RedeemedByUserId = redeemedByUserId;
        await db.SaveChangesAsync(ct);
        return invite;
    }

    public async Task<bool> RevokeAsync(int id, string revokedByUserId, CancellationToken ct = default)
    {
        InviteCode? invite = await db.InviteCodes.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (invite is null || invite.IsRedeemed || invite.IsRevoked)
        {
            return false;
        }

        invite.RevokedAt = timeProvider.GetUtcNow();
        invite.RevokedByUserId = revokedByUserId;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public IQueryable<InviteCode> Query() => db.InviteCodes.AsNoTracking();

    private static string GenerateCode()
    {
        Span<byte> bytes = stackalloc byte[CodeBytes];
        RandomNumberGenerator.Fill(bytes);
        // Base64Url, no padding -> 12 chars from 9 bytes
        string raw = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        // Format like Vk7n-9pQ2-rTxW for readability
        return $"{raw[..4]}-{raw[4..8]}-{raw[8..12]}";
    }
}
