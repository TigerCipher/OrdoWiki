namespace OrdoWiki.Data.Entities;

/// <summary>
/// Singleton row holding the live theme. Palette overrides for light/dark, plus optional
/// per-mode background images and a free-form dictionary of custom CSS variable values.
/// </summary>
public class SiteTheme
{
    /// <summary>Fixed singleton id so the row is easy to look up.</summary>
    public static readonly Guid SingletonId = Guid.Parse("51000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }

    /// <summary>JSON dictionary&lt;palette-key, color-hex&gt;. Empty means "use defaults".</summary>
    public string LightPaletteJson { get; set; } = "{}";

    public string DarkPaletteJson { get; set; } = "{}";

    /// <summary>JSON dictionary&lt;variable-name, {Light,Dark}&gt; for admin-registered custom CSS vars.</summary>
    public string CustomValuesJson { get; set; } = "{}";

    public Guid? LightBackgroundAssetId { get; set; }
    public MediaAsset? LightBackgroundAsset { get; set; }

    public Guid? DarkBackgroundAssetId { get; set; }
    public MediaAsset? DarkBackgroundAsset { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedById { get; set; }
    public ApplicationUser? UpdatedBy { get; set; }
}
