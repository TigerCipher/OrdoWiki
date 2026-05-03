namespace OrdoWiki.Web.Helpers;

using Data.Entities;

public static class Mappings
{
    public static UserDto MapToDto(ApplicationUser user) =>
        new()
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            IsPasswordResetRequired = user.IsPasswordResetRequired
        };

    public static WikiPageDto MapToDto(WikiPage page, PageRevision currentRevision) =>
        new()
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
                EditedAt = currentRevision.EditedAt,
                EditedById = currentRevision.EditedById,
                Editor = currentRevision.Editor is null ? null : MapToDto(currentRevision.Editor)
            },
            CreatedAt = page.CreatedAt,
            CreatedById = page.CreatedById,
            Creator = page.Creator is null ? null : MapToDto(page.Creator)
        };
}