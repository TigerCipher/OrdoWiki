namespace OrdoWiki.Web.Services.Contract;

using Models;

public interface ISearchService
{
    /// <summary>
    /// Full-text search across logs, characters, and timeline events. Returns up to
    /// <paramref name="limit"/> matches per category, ranked by relevance.
    /// </summary>
    Task<SearchResultsDto> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default);
}
