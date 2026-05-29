namespace OrdoWiki.Web.Services.Contract;

using Data.Entities;
using Models;
using Models.Requests;

public interface IRelatedItemsService
{
    Task<RelatedItemsDto> GetForAsync(RelatedItemKind kind, Guid id);

    Task<ApiResponse<RelatedItemsDto>> SetForAsync(RelatedItemKind kind, Guid id, SetRelatedItemsRequest request);

    Task<IReadOnlyList<RelatedItemRef>> SearchAsync(RelatedItemKind kind, string? query, int limit = 50);

    Task DeleteAllForAsync(RelatedItemKind kind, Guid id);
}
