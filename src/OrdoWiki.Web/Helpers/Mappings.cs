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

    public static CharacterImageDto MapToDto(CharacterImage image) =>
        new()
        {
            Id = image.Id,
            CharacterId = image.CharacterId,
            MediaAssetId = image.MediaAssetId,
            Caption = image.Caption,
            OrderIndex = image.OrderIndex,
            MediaAsset = image.MediaAsset is null ? null : MapToDto(image.MediaAsset)
        };

    public static CharacterDto MapToDto(Character character, string? ownerRole = null) =>
        new()
        {
            Id = character.Id,
            Slug = character.Slug,
            Name = character.Name,
            Summary = character.Summary,
            MarkdownBody = character.MarkdownBody,
            OwnerId = character.OwnerId,
            Owner = character.Owner is null ? null : MapToDto(character.Owner, ownerRole),
            CreatedAt = AsUtc(character.CreatedAt),
            UpdatedAt = AsUtc(character.UpdatedAt),
            Images = character.Images
                .OrderBy(i => i.OrderIndex)
                .Select(MapToDto)
                .ToList()
        };

    public static TimelineEventDto MapToDto(TimelineEvent ev, string? createdByRole = null, string displayDate = "") =>
        new()
        {
            Id = ev.Id,
            Title = ev.Title,
            MarkdownBody = ev.MarkdownBody,
            EpochDayNumber = ev.EpochDayNumber,
            MandoYear = ev.MandoYear,
            MandoMonth = ev.MandoMonth,
            MandoDay = ev.MandoDay,
            DisplayOverride = ev.DisplayOverride,
            CreatedById = ev.CreatedById,
            CreatedBy = ev.CreatedBy is null ? null : MapToDto(ev.CreatedBy, createdByRole),
            CreatedAt = AsUtc(ev.CreatedAt),
            UpdatedAt = AsUtc(ev.UpdatedAt),
            DisplayDate = displayDate,
        };

    public static MandoMonthDto MapToDto(MandoMonth month) =>
        new()
        {
            Id = month.Id,
            MonthIndex = month.MonthIndex,
            Name = month.Name,
        };

    public static MandoEraDto MapToDto(MandoEra era) =>
        new()
        {
            Id = era.Id,
            Name = era.Name,
            ShortCode = era.ShortCode,
            AnchorYear = era.AnchorYear,
            Direction = era.Direction,
            SortOrder = era.SortOrder,
        };

    public static MediaAssetDto MapToDto(MediaAsset asset, string? uploaderRole = null) =>
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
            UploadedBy = asset.UploadedBy is null ? null : MapToDto(asset.UploadedBy, uploaderRole),
            UploadedAt = AsUtc(asset.UploadedAt),
            SourceType = asset.SourceType,
            SourceId = asset.SourceId
        };

    // Npgsql returns timestamps with Kind=Unspecified; the wire value is UTC.
    // Tag it so downstream code (JS interop, TimeZoneInfo.ConvertTimeFromUtc) doesn't misinterpret.
    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}