namespace OrdoWiki.Web.Services;

/// <summary>
/// Single source of truth for upload byte caps.
/// Both server-side validation (MediaService) and client-side IBrowserFile
/// stream limits must reference these — if they drift, oversized files crash
/// mid-stream instead of being rejected up front.
/// </summary>
public static class MediaLimits
{
    /// <summary>Max bytes accepted for a generic image upload (gallery, character, embed).</summary>
    public const long MaxImageBytes = 50L * 1024 * 1024;

    /// <summary>Max bytes accepted for a profile avatar before the center-crop pass.</summary>
    public const long MaxAvatarBytes = 10L * 1024 * 1024;
}
