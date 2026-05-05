namespace OrdoWiki.Web.Services;

using Data;
using Data.Entities;
using Exceptions;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models.Requests;

public class PageService(
    ApplicationDbContext context,
    IUserService userService) : IPageService
{
    public async Task<ApiResponse<WikiPageDto>> GetPageByIdAsync(Guid id)
    {
        WikiPage? page = await context.WikiPages.AsNoTracking()
            .Include(x => x.CurrentRevision)
            .ThenInclude(r => r!.Editor)
            .Include(x => x.Creator)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (page?.CurrentRevision is null)
            return NotFound<WikiPageDto>();

        Dictionary<string, string?> roles = await LoadRolesForPageAsync(page);
        return Ok(MapToDto(page, page.CurrentRevision, roles));
    }

    public async Task<ApiResponse<WikiPageDto>> GetPageBySlugAsync(string slug)
    {
        WikiPage? page = await context.WikiPages.AsNoTracking()
            .Include(x => x.CurrentRevision)
            .ThenInclude(r => r!.Editor)
            .Include(x => x.Creator)
            .SingleOrDefaultAsync(x => x.Slug == slug);
        if (page?.CurrentRevision is null)
            return NotFound<WikiPageDto>();

        Dictionary<string, string?> roles = await LoadRolesForPageAsync(page);
        return Ok(MapToDto(page, page.CurrentRevision, roles));
    }

    public async Task<ApiResponse<WikiPageDto>> CreatePageAsync(CreatePageRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest<WikiPageDto>("Title is required");

            if (string.IsNullOrWhiteSpace(request.MarkdownBody))
                return BadRequest<WikiPageDto>("Body is required");

            ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
            if (!userResponse)
            {
                return BadRequest<WikiPageDto>($"User not found - {userResponse.Error}");
            }

            UserDto user = userResponse;

            string baseSlug = string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug;
            baseSlug = baseSlug.CreateSlug();
            if (string.IsNullOrWhiteSpace(baseSlug))
                return BadRequest<WikiPageDto>("Title produced an empty slug");

            string slug = await EnsureUniqueSlugAsync(baseSlug);
            DateTime createdAt = DateTime.UtcNow;

            WikiPage page = new()
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Slug = slug,
                CreatedAt = createdAt,
                CreatedById = user.Id,
                Summary = request.Summary?.Trim() ?? null
            };

            PageRevision revision = new()
            {
                Id = Guid.NewGuid(),
                PageId = page.Id,
                MarkdownBody = request.MarkdownBody,
                EditSummary = request.EditSummary,
                EditedAt = createdAt,
                EditedById = user.Id
            };

            await using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();

            context.WikiPages.Add(page);
            context.PageRevisions.Add(revision);
            await context.SaveChangesAsync();

            page.CurrentRevisionId = revision.Id;
            await context.SaveChangesAsync();

            await tx.CommitAsync();

            return Ok(MapToDto(page, revision));
        }
        catch (OrdoException ex)
        {
            return BadRequest<WikiPageDto>(ex.Message);
        }
    }

    public async Task<ApiResponse<WikiPageDto>> EditPageAsync(EditPageRequest request)
    {
        try
        {
            WikiPage? page = await context.WikiPages
                .SingleOrDefaultAsync(x => x.Id == request.PageId);
            if (page is null)
                return NotFound<WikiPageDto>();
        
            ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
            if (!userResponse)
            {
                return BadRequest<WikiPageDto>($"User not found - {userResponse.Error}");
            }

            UserDto user = userResponse;
        
            PageRevision revision = new()
            {
                Id = Guid.NewGuid(),
                PageId = page.Id,
                MarkdownBody = request.MarkdownBody,
                EditSummary = request.EditSummary,
                EditedAt = DateTime.UtcNow,
                EditedById = user.Id
            };
        
            context.PageRevisions.Add(revision);
            page.CurrentRevisionId = revision.Id;
        
            page.Title = request.Title.Trim();
            page.Summary = request.Summary?.Trim() ?? null;

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                string normalized = request.Slug.CreateSlug();
                if(string.IsNullOrWhiteSpace(normalized))
                    return BadRequest<WikiPageDto>("Slug is invalid");
            
                if(normalized != page.Slug)
                    page.Slug = await EnsureUniqueSlugAsync(normalized);
            }
        
            await context.SaveChangesAsync();
        
            return Ok(MapToDto(page, revision));
        }
        catch (OrdoException ex)
        {
            return BadRequest<WikiPageDto>(ex.Message);
        }
    }

    public async Task<ApiResponse<List<WikiPageDto>>> GetPagesAsync()
    {
        List<WikiPage> pages = await context.WikiPages
            .AsNoTracking()
            .Include(x => x.CurrentRevision)
            .ThenInclude(r => r!.Editor)
            .Include(x => x.Creator)
            .ToListAsync();

        IEnumerable<string> userIds = pages
            .SelectMany(p => new[] { p.CreatedById, p.CurrentRevision?.EditedById })
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!);

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(userIds);

        List<WikiPageDto> dtos = pages
            .Where(p => p.CurrentRevision != null)
            .Select(p => MapToDto(p, p.CurrentRevision!, roles))
            .ToList();

        return Ok(dtos);
    }

    private Task<Dictionary<string, string?>> LoadRolesForPageAsync(WikiPage page)
    {
        List<string> ids = [page.CreatedById];
        if (page.CurrentRevision?.EditedById is { } editedById)
            ids.Add(editedById);

        return userService.GetHighestRolesAsync(ids);
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        HashSet<string> taken = await context.WikiPages
            .AsNoTracking()
            .Where(x => x.Slug == baseSlug || x.Slug.StartsWith(baseSlug + "-"))
            .Select(x => x.Slug)
            .ToHashSetAsync();

        if (!taken.Contains(baseSlug))
            return baseSlug;

        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{baseSlug}-{i}";
            if (!taken.Contains(candidate)) return candidate;
        }

        throw new OrdoException($"Could not generate a unique slug for '{baseSlug}'");
    }
}