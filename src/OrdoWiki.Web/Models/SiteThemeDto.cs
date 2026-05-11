namespace OrdoWiki.Web.Models;

public sealed class SiteThemeDto
{
    /// <summary>Palette overrides keyed by Mud palette property name (e.g. "Primary").</summary>
    public Dictionary<string, string> LightPalette { get; set; } = new();

    public Dictionary<string, string> DarkPalette { get; set; } = new();

    /// <summary>Custom CSS variable values keyed by variable name (e.g. "--mando-trim-color").</summary>
    public Dictionary<string, ThemeValuePair> CustomValues { get; set; } = new();

    public Guid? LightBackgroundAssetId { get; set; }
    public string? LightBackgroundUrl { get; set; }

    public Guid? DarkBackgroundAssetId { get; set; }
    public string? DarkBackgroundUrl { get; set; }
}

public sealed class ThemeValuePair
{
    public string? Light { get; set; }
    public string? Dark { get; set; }
}

public sealed class CustomThemeVariableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
