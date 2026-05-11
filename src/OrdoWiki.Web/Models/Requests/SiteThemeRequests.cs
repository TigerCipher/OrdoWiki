namespace OrdoWiki.Web.Models.Requests;

public sealed class SaveSiteThemeRequest
{
    public Dictionary<string, string> LightPalette { get; set; } = new();
    public Dictionary<string, string> DarkPalette { get; set; } = new();
    public Dictionary<string, ThemeValuePair> CustomValues { get; set; } = new();
    public Guid? LightBackgroundAssetId { get; set; }
    public Guid? DarkBackgroundAssetId { get; set; }
}

public sealed class CreateCustomVariableRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public sealed class UpdateCustomVariableRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
