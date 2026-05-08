namespace OrdoWiki.Web.Helpers;

using Data.Entities;

public static class Mappings
{
    public static UserDto MapToDto(ApplicationUser user, string? role = null) =>
        new()
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            AvatarPath = user.AvatarPath,
            Role = role,
            IsPasswordResetRequired = user.IsPasswordResetRequired
        };

    public static WikiPageDto MapToDto(
        WikiPage page,
        PageRevision currentRevision,
        IReadOnlyDictionary<string, string?>? rolesByUserId = null)
    {
        string? RoleFor(string? userId) =>
            userId is not null && rolesByUserId is not null && rolesByUserId.TryGetValue(userId, out string? r)
                ? r
                : null;

        return new WikiPageDto
        {
            Id = page.Id,
            Slug = page.Slug,
            Title = page.Title,
            Summary = page.Summary,
            CurrentRevisionId = currentRevision.Id,
            CurrentRevision = new PageRevisionDto
            {
                Id = currentRevision.Id,
                PageId = page.Id,
                MarkdownBody = currentRevision.MarkdownBody,
                EditSummary = currentRevision.EditSummary,
                EditedAt = AsUtc(currentRevision.EditedAt),
                EditedById = currentRevision.EditedById,
                Editor = currentRevision.Editor is null
                    ? null
                    : MapToDto(currentRevision.Editor, RoleFor(currentRevision.Editor.Id))
            },
            CreatedAt = AsUtc(page.CreatedAt),
            CreatedById = page.CreatedById,
            Creator = page.Creator is null
                ? null
                : MapToDto(page.Creator, RoleFor(page.Creator.Id))
        };
    }

    public static MediaAssetDto MapToDto(MediaAsset asset) =>
        new()
        {
            Id = asset.Id,
            StoragePath = asset.StoragePath,
            OriginalName = asset.OriginalName,
            ContentType = asset.ContentType,
            SizeBytes = asset.SizeBytes,
            Width = asset.Width,
            Height = asset.Height,
            UploadedById = asset.UploadedById,
            UploadedAt = AsUtc(asset.UploadedAt)
        };

    // Npgsql returns timestamps with Kind=Unspecified; the wire value is UTC.
    // Tag it so downstream code (JS interop, TimeZoneInfo.ConvertTimeFromUtc) doesn't misinterpret.
    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}