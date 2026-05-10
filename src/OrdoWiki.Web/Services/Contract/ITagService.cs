namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface ITagService
{
    /// <summary>All tags, alphabetical. Includes usage count across all surfaces.</summary>
    Task<IReadOnlyList<TagDto>> GetAllAsync();

    /// <summary>Autocomplete-style search by name prefix or substring.</summary>
    Task<IReadOnlyList<TagDto>> SearchAsync(string query, int limit = 20);

    /// <summary>Get or create a tag by display name (slug derived from the name).</summary>
    Task<ApiResponse<TagDto>> GetOrCreateAsync(string name);

    Task<IReadOnlyList<TagDto>> GetTagsForAsync(TagTarget target, Guid entityId);

    /// <summary>
    /// Replace the tag set for the given entity with the given list of names.
    /// New names are auto-created. Removed names are detached but the tag itself stays.
    /// </summary>
    Task<ApiResponse<bool>> SetTagsAsync(TagTarget target, Guid entityId, IEnumerable<string> tagNames);
}
