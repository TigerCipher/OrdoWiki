namespace OrdoWiki.Web.Services;

using System.Text;
using Data;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Models;
using NpgsqlTypes;

public class SearchService(ApplicationDbContext context) : ISearchService
{
    private const int SnippetWindow = 80;

    public async Task<SearchResultsDto> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        SearchResultsDto empty = new() { Query = query ?? string.Empty };
        string trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length < 2) return empty;

        string[] terms = SplitTerms(trimmed);
        string tsQuery = BuildPrefixTsQuery(terms);
        // Every term in the query becomes a prefix match (`term:*`), so "thun" finds
        // "thunvu" without the user having to type the whole word. We lose the
        // websearch operators (quotes, OR, -negation) in exchange — fine for a wiki
        // search where users just type names and keywords.
        if (string.IsNullOrEmpty(tsQuery)) return empty;

        List<SearchResultDto> logs = await SearchLogsAsync(tsQuery, terms, limit, cancellationToken);
        List<SearchResultDto> characters = await SearchCharactersAsync(tsQuery, terms, limit, cancellationToken);
        List<SearchResultDto> events = await SearchEventsAsync(tsQuery, terms, limit, cancellationToken);

        return new SearchResultsDto
        {
            Query = trimmed,
            Logs = logs,
            Characters = characters,
            Events = events,
        };
    }

    private async Task<List<SearchResultDto>> SearchLogsAsync(string query, string[] terms, int limit, CancellationToken ct)
    {
        var rows = await context.WikiPages
            .AsNoTracking()
            .Where(p => p.SearchVector.Matches(EF.Functions.ToTsQuery("english", query))
                     || (p.CurrentRevision != null
                         && p.CurrentRevision.SearchVector.Matches(EF.Functions.ToTsQuery("english", query))))
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Summary,
                Body = p.CurrentRevision != null ? p.CurrentRevision.MarkdownBody : null,
                TitleRank = p.SearchVector.Rank(EF.Functions.ToTsQuery("english", query)),
                BodyRank = p.CurrentRevision != null
                    ? p.CurrentRevision.SearchVector.Rank(EF.Functions.ToTsQuery("english", query))
                    : 0f,
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new SearchResultDto
            {
                Kind = SearchResultKind.Log,
                Id = r.Id,
                Title = r.Title,
                Subtitle = r.Summary,
                Href = $"/logs/{r.Slug}",
                Rank = r.TitleRank + r.BodyRank,
                SnippetHtml = BuildSnippet(terms, r.Body, r.Summary),
            })
            .OrderByDescending(r => r.Rank)
            .Take(limit)
            .ToList();
    }

    private async Task<List<SearchResultDto>> SearchCharactersAsync(string query, string[] terms, int limit, CancellationToken ct)
    {
        var rows = await context.Characters
            .AsNoTracking()
            .Where(c => c.SearchVector.Matches(EF.Functions.ToTsQuery("english", query)))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.Summary,
                c.MarkdownBody,
                Rank = c.SearchVector.Rank(EF.Functions.ToTsQuery("english", query)),
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new SearchResultDto
            {
                Kind = SearchResultKind.Character,
                Id = r.Id,
                Title = r.Name,
                Subtitle = r.Summary,
                Href = $"/characters/{r.Slug}",
                Rank = r.Rank,
                SnippetHtml = BuildSnippet(terms, r.MarkdownBody, r.Summary),
            })
            .OrderByDescending(r => r.Rank)
            .Take(limit)
            .ToList();
    }

    private async Task<List<SearchResultDto>> SearchEventsAsync(string query, string[] terms, int limit, CancellationToken ct)
    {
        var rows = await context.TimelineEvents
            .AsNoTracking()
            .Where(e => e.SearchVector.Matches(EF.Functions.ToTsQuery("english", query)))
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Summary,
                e.MarkdownBody,
                e.DisplayOverride,
                e.MandoYear,
                Rank = e.SearchVector.Rank(EF.Functions.ToTsQuery("english", query)),
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new SearchResultDto
            {
                Kind = SearchResultKind.TimelineEvent,
                Id = r.Id,
                Title = r.Title,
                Subtitle = r.DisplayOverride ?? r.Summary,
                Href = $"/timeline/{r.Id}",
                Rank = r.Rank,
                SnippetHtml = BuildSnippet(terms, r.MarkdownBody, r.Summary),
            })
            .OrderByDescending(r => r.Rank)
            .Take(limit)
            .ToList();
    }

    private static string[] SplitTerms(string query)
    {
        // For client-side snippet highlighting we approximate Postgres's tokenization
        // by splitting on whitespace and stripping basic punctuation. Stemming is not
        // replicated — exact-form matches are highlighted, near-matches won't be.
        return query
            .Split([' ', '\t', '\n', ',', '.', ';', ':', '!', '?', '"', '\''], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2 && !t.StartsWith('-'))
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToArray();
    }

    private static string BuildPrefixTsQuery(string[] terms)
    {
        // to_tsquery() is strict — only ANDed/ORed tokens with valid syntax.
        // Sanitize each term to letters/digits/_/- so we can hand it straight to
        // Postgres without risk of injection or parse errors, then append :* for
        // prefix matching.
        IEnumerable<string> sanitized = terms
            .Select(t => new string(t.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray()))
            .Where(t => t.Length > 0)
            .Select(t => $"{t}:*");

        return string.Join(" & ", sanitized);
    }

    private static string? BuildSnippet(string[] terms, string? body, string? summary)
    {
        string source = !string.IsNullOrWhiteSpace(body)
            ? body
            : summary ?? string.Empty;
        if (string.IsNullOrWhiteSpace(source) || terms.Length == 0) return null;

        // Find the earliest hit position among any term so the snippet centers
        // around something the user can see, not just the first N chars.
        int hitIndex = -1;
        foreach (string term in terms)
        {
            int idx = source.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && (hitIndex == -1 || idx < hitIndex)) hitIndex = idx;
        }

        int start, length;
        if (hitIndex < 0)
        {
            // No exact substring hit (Postgres found it via stemming). Fall back to
            // the head of the content so we always return something useful.
            start = 0;
            length = Math.Min(source.Length, SnippetWindow * 2);
        }
        else
        {
            start = Math.Max(0, hitIndex - SnippetWindow);
            length = Math.Min(source.Length - start, SnippetWindow * 2);
        }

        string slice = source.Substring(start, length);
        string ellipsisPrefix = start > 0 ? "…" : string.Empty;
        string ellipsisSuffix = start + length < source.Length ? "…" : string.Empty;

        return ellipsisPrefix + Highlight(slice, terms) + ellipsisSuffix;
    }

    private static string Highlight(string text, string[] terms)
    {
        string escaped = System.Net.WebUtility.HtmlEncode(text);
        if (terms.Length == 0) return escaped;

        StringBuilder pattern = new();
        for (int i = 0; i < terms.Length; i++)
        {
            if (i > 0) pattern.Append('|');
            pattern.Append(System.Text.RegularExpressions.Regex.Escape(System.Net.WebUtility.HtmlEncode(terms[i])));
        }

        return System.Text.RegularExpressions.Regex.Replace(
            escaped,
            pattern.ToString(),
            m => $"<mark>{m.Value}</mark>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
