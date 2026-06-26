namespace OrdoWiki.Web.Services;

using Data;
using Data.Auth;
using Data.Entities;
using Exceptions;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Models.Requests;

public class CharacterService(
    ApplicationDbContext context,
    IUserService userService,
    IMediaService mediaService,
    ITagService tagService,
    IRelatedItemsService relatedItemsService) : ICharacterService
{
    public async Task<ApiResponse<CharacterDto>> GetCharacterByIdAsync(Guid id)
    {
        Character? character = await context.Characters
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images.OrderBy(i => i.OrderIndex))
                .ThenInclude(i => i.MediaAsset)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (character is null) return NotFound<CharacterDto>();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync([character.OwnerId]);
        roles.TryGetValue(character.OwnerId, out string? ownerRole);

        CharacterDto dto = MapToDto(character, ownerRole);
        dto.Tags = await tagService.GetTagsForAsync(TagTarget.Character, character.Id);
        return Ok(dto);
    }

    public async Task<ApiResponse<CharacterDto>> GetCharacterBySlugAsync(string slug)
    {
        Character? character = await context.Characters
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images.OrderBy(i => i.OrderIndex))
                .ThenInclude(i => i.MediaAsset)
            .SingleOrDefaultAsync(c => c.Slug == slug);

        if (character is null) return NotFound<CharacterDto>();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync([character.OwnerId]);
        roles.TryGetValue(character.OwnerId, out string? ownerRole);

        CharacterDto dto = MapToDto(character, ownerRole);
        dto.Tags = await tagService.GetTagsForAsync(TagTarget.Character, character.Id);
        return Ok(dto);
    }

    public async Task<ApiResponse<List<CharacterDto>>> GetCharactersAsync(Guid? tagId = null)
    {
        IQueryable<Character> query = context.Characters
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images.OrderBy(i => i.OrderIndex).Take(1))
                .ThenInclude(i => i.MediaAsset);

        if (tagId.HasValue)
        {
            Guid id = tagId.Value;
            query = query.Where(c => context.CharacterTags.Any(j => j.CharacterId == c.Id && j.TagId == id));
        }

        List<Character> characters = await query
            .OrderBy(c => c.Owner!.DisplayName ?? c.Owner.UserName)
            .ThenBy(c => c.Name)
            .ToListAsync();

        Dictionary<string, string?> roles = await userService.GetHighestRolesAsync(
            characters.Select(c => c.OwnerId));

        List<CharacterDto> dtos = characters
            .Select(c => MapToDto(c, roles.GetValueOrDefault(c.OwnerId)))
            .ToList();

        return Ok(dtos);
    }

    public async Task<ApiResponse<List<CharacterDto>>> GetMyCharactersAsync()
    {
        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<List<CharacterDto>>(userResponse.Error);

        UserDto user = userResponse;

        List<Character> characters = await context.Characters
            .AsNoTracking()
            .Include(c => c.Owner)
            .Include(c => c.Images.OrderBy(i => i.OrderIndex).Take(1))
                .ThenInclude(i => i.MediaAsset)
            .Where(c => c.OwnerId == user.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        List<CharacterDto> dtos = characters
            .Select(c => MapToDto(c, user.Role))
            .ToList();

        return Ok(dtos);
    }

    public async Task<ApiResponse<CharacterDto>> CreateCharacterAsync(CreateCharacterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest<CharacterDto>("Name is required");

            ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
            if (!userResponse) return Unauthorized<CharacterDto>(userResponse.Error);

            UserDto user = userResponse;

            if (!IsPrivileged(user.Role))
            {
                int existing = await context.Characters.CountAsync(c => c.OwnerId == user.Id);
                if (existing >= CharacterCaps.ReaderMaxCharacters)
                    return Forbidden<CharacterDto>(
                        $"You have reached the limit of {CharacterCaps.ReaderMaxCharacters} characters.");
            }

            string baseSlug = string.IsNullOrWhiteSpace(request.Slug) ? request.Name : request.Slug;
            baseSlug = baseSlug.CreateSlug();
            if (string.IsNullOrWhiteSpace(baseSlug))
                return BadRequest<CharacterDto>("Name produced an empty slug");

            string slug = await EnsureUniqueSlugAsync(baseSlug);
            DateTime now = DateTime.UtcNow;

            Character character = new()
            {
                Id = request.Id == Guid.Empty ? Guid.NewGuid() : request.Id,
                Slug = slug,
                Name = request.Name.Trim(),
                Summary = request.Summary?.Trim(),
                MarkdownBody = request.MarkdownBody ?? string.Empty,
                OwnerId = user.Id,
                CreatedAt = now,
                UpdatedAt = now,
            };

            context.Characters.Add(character);
            await context.SaveChangesAsync();

            if (request.Tags is not null)
                await tagService.SetTagsAsync(TagTarget.Character, character.Id, request.Tags);

            return await GetCharacterByIdAsync(character.Id);
        }
        catch (OrdoException ex)
        {
            return BadRequest<CharacterDto>(ex.Message);
        }
    }

    public async Task<ApiResponse<CharacterDto>> EditCharacterAsync(EditCharacterRequest request)
    {
        try
        {
            Character? character = await context.Characters
                .SingleOrDefaultAsync(c => c.Id == request.CharacterId);
            if (character is null) return NotFound<CharacterDto>();

            ApiResponse<bool> editCheck = await CanEditCharacterAsync(character.Id);
            if (!editCheck.Success) return Forbidden<CharacterDto>(editCheck.Error);

            character.Name = request.Name.Trim();
            character.Summary = request.Summary?.Trim();
            character.MarkdownBody = request.MarkdownBody ?? string.Empty;
            character.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                string normalized = request.Slug.CreateSlug();
                if (string.IsNullOrWhiteSpace(normalized))
                    return BadRequest<CharacterDto>("Slug is invalid");

                if (normalized != character.Slug)
                    character.Slug = await EnsureUniqueSlugAsync(normalized);
            }

            await context.SaveChangesAsync();

            if (request.Tags is not null)
                await tagService.SetTagsAsync(TagTarget.Character, character.Id, request.Tags);

            return await GetCharacterByIdAsync(character.Id);
        }
        catch (OrdoException ex)
        {
            return BadRequest<CharacterDto>(ex.Message);
        }
    }

    public async Task<ApiResponse<bool>> DeleteCharacterAsync(Guid characterId)
    {
        Character? character = await context.Characters
            .SingleOrDefaultAsync(c => c.Id == characterId);
        if (character is null) return NotFound<bool>();

        ApiResponse<bool> editCheck = await CanEditCharacterAsync(character.Id);
        if (!editCheck.Success) return Forbidden<bool>(editCheck.Error);

        await relatedItemsService.DeleteAllForAsync(Data.Entities.RelatedItemKind.Character, character.Id);

        context.Characters.Remove(character);
        await context.SaveChangesAsync();
        return Ok(true);
    }

    public async Task<ApiResponse<CharacterImageDto>> AttachImageAsync(
        Guid characterId,
        Stream input,
        string originalName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        Character? character = await context.Characters
            .Include(c => c.Images)
            .SingleOrDefaultAsync(c => c.Id == characterId, cancellationToken);
        if (character is null) return NotFound<CharacterImageDto>();

        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<CharacterImageDto>(userResponse.Error);

        UserDto user = userResponse;

        if (!CanEdit(user, character))
            return Forbidden<CharacterImageDto>("You cannot edit this character.");

        if (!IsPrivileged(user.Role) && character.Images.Count >= CharacterCaps.ReaderMaxImagesPerCharacter)
            return Forbidden<CharacterImageDto>(
                $"This character already has the maximum of {CharacterCaps.ReaderMaxImagesPerCharacter} images.");

        ApiResponse<MediaAssetDto> uploadResponse = await mediaService.UploadImageAsync(
            input, originalName, contentType, sizeBytes,
            MediaSourceType.Character, character.Id, cancellationToken);
        if (!uploadResponse) return BadRequest<CharacterImageDto>(uploadResponse.Error ?? "Upload failed.");

        MediaAssetDto asset = uploadResponse;

        int nextOrder = character.Images.Count == 0
            ? 0
            : character.Images.Max(i => i.OrderIndex) + 1;

        CharacterImage row = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = character.Id,
            MediaAssetId = asset.Id,
            OrderIndex = nextOrder,
        };

        context.CharacterImages.Add(row);
        await context.SaveChangesAsync(cancellationToken);

        // Re-fetch with the asset populated for the dto.
        CharacterImage? saved = await context.CharacterImages
            .AsNoTracking()
            .Include(i => i.MediaAsset)
            .SingleOrDefaultAsync(i => i.Id == row.Id, cancellationToken);
        if (saved is null) return NotFound<CharacterImageDto>();

        return Ok(MapToDto(saved));
    }

    public async Task<ApiResponse<bool>> RemoveImageAsync(Guid imageId)
    {
        CharacterImage? image = await context.CharacterImages
            .Include(i => i.Character)
            .Include(i => i.MediaAsset)
            .SingleOrDefaultAsync(i => i.Id == imageId);
        if (image is null) return NotFound<bool>();

        ApiResponse<bool> editCheck = await CanEditCharacterAsync(image.CharacterId);
        if (!editCheck.Success) return Forbidden<bool>(editCheck.Error);

        // Each character image owns its underlying MediaAsset 1:1 (a fresh upload
        // per AddImage call). Removing the join row alone would orphan the asset
        // in the gallery, so we drop the asset + its tags + the file too.
        Guid assetId = image.MediaAssetId;
        string storagePath = image.MediaAsset.StoragePath;

        // Drop the join row first so the FK Restrict on MediaAsset doesn't fire
        // when we delete the asset itself.
        context.CharacterImages.Remove(image);
        await context.SaveChangesAsync();

        await context.MediaAssetTags.Where(j => j.MediaAssetId == assetId).ExecuteDeleteAsync();
        await context.MediaAssets.Where(a => a.Id == assetId).ExecuteDeleteAsync();

        mediaService.TryDeleteFile(storagePath);
        return Ok(true);
    }

    public async Task<ApiResponse<bool>> ReorderImagesAsync(ReorderCharacterImagesRequest request)
    {
        ApiResponse<bool> editCheck = await CanEditCharacterAsync(request.CharacterId);
        if (!editCheck.Success) return Forbidden<bool>(editCheck.Error);

        List<CharacterImage> images = await context.CharacterImages
            .Where(i => i.CharacterId == request.CharacterId)
            .ToListAsync();

        Dictionary<Guid, int> orderById = request.Order.ToDictionary(o => o.ImageId, o => o.OrderIndex);

        foreach (CharacterImage image in images)
        {
            if (orderById.TryGetValue(image.Id, out int orderIndex))
                image.OrderIndex = orderIndex;
        }

        await context.SaveChangesAsync();
        return Ok(true);
    }

    public async Task<ApiResponse<CharacterImageDto>> UpdateImageCaptionAsync(UpdateImageCaptionRequest request)
    {
        CharacterImage? image = await context.CharacterImages
            .Include(i => i.MediaAsset)
            .SingleOrDefaultAsync(i => i.Id == request.ImageId);
        if (image is null) return NotFound<CharacterImageDto>();

        ApiResponse<bool> editCheck = await CanEditCharacterAsync(image.CharacterId);
        if (!editCheck.Success) return Forbidden<CharacterImageDto>(editCheck.Error);

        image.Caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim();
        await context.SaveChangesAsync();

        return Ok(MapToDto(image));
    }

    public async Task<ApiResponse<bool>> CanCreateCharacterAsync()
    {
        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<bool>(userResponse.Error);

        UserDto user = userResponse;

        if (IsPrivileged(user.Role)) return Ok(true);

        int existing = await context.Characters.CountAsync(c => c.OwnerId == user.Id);
        return Ok(existing < CharacterCaps.ReaderMaxCharacters);
    }

    public async Task<ApiResponse<bool>> CanEditCharacterAsync(Guid characterId)
    {
        Character? character = await context.Characters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == characterId);
        if (character is null) return NotFound<bool>();

        ApiResponse<UserDto> userResponse = await userService.GetCurrentUserAsync();
        if (!userResponse) return Unauthorized<bool>(userResponse.Error);

        UserDto user = userResponse;
        return Ok(CanEdit(user, character));
    }

    private static bool CanEdit(UserDto user, Character character) =>
        IsPrivileged(user.Role) || string.Equals(user.Id, character.OwnerId, StringComparison.Ordinal);

    private static bool IsPrivileged(string? role) =>
        string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Roles.Designer, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(role, Roles.Editor, StringComparison.OrdinalIgnoreCase);

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        HashSet<string> taken = await context.Characters
            .AsNoTracking()
            .Where(c => c.Slug == baseSlug || c.Slug.StartsWith(baseSlug + "-"))
            .Select(c => c.Slug)
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
