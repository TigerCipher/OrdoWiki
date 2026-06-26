namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface ICharacterService
{
    Task<ApiResponse<CharacterDto>> GetCharacterByIdAsync(Guid id);
    Task<ApiResponse<CharacterDto>> GetCharacterBySlugAsync(string slug);
    Task<ApiResponse<List<CharacterDto>>> GetCharactersAsync(Guid? tagId = null);
    Task<ApiResponse<List<CharacterDto>>> GetMyCharactersAsync();

    Task<ApiResponse<CharacterDto>> CreateCharacterAsync(CreateCharacterRequest request);
    Task<ApiResponse<CharacterDto>> EditCharacterAsync(EditCharacterRequest request);
    Task<ApiResponse<bool>> DeleteCharacterAsync(Guid characterId);

    Task<ApiResponse<CharacterImageDto>> AttachImageAsync(
        Guid characterId,
        Stream input,
        string originalName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CharacterImageDto>> AttachExistingImageAsync(
        Guid characterId,
        Guid sourceAssetId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<bool>> RemoveImageAsync(Guid imageId);
    Task<ApiResponse<bool>> ReorderImagesAsync(ReorderCharacterImagesRequest request);
    Task<ApiResponse<CharacterImageDto>> UpdateImageCaptionAsync(UpdateImageCaptionRequest request);

    Task<ApiResponse<bool>> CanCreateCharacterAsync();
    Task<ApiResponse<bool>> CanEditCharacterAsync(Guid characterId);
}
