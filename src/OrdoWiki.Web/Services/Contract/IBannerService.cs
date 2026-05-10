namespace OrdoWiki.Web.Services.Contract;

using Models;
using Models.Requests;

public interface IBannerService
{
    /// <summary>All 4 slot rows ordered by SlotIndex (filled or empty). For management UI.</summary>
    Task<IReadOnlyList<BannerDto>> GetSlotsAsync();

    /// <summary>Only filled banners ordered by SlotIndex. For the public carousel.</summary>
    Task<IReadOnlyList<BannerDto>> GetVisibleAsync();

    /// <summary>
    /// Update the contents of a single slot. Slot 4 requires Admin role; slots 1-3 require CanDesign.
    /// Pass <c>MediaAssetId = null</c> to clear the slot.
    /// </summary>
    Task<ApiResponse<BannerDto>> SetAsync(SetBannerRequest request);
}
