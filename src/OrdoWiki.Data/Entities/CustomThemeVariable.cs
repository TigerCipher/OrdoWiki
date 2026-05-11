namespace OrdoWiki.Data.Entities;

/// <summary>
/// Admin-managed registry of additional CSS variables that designers can override per mode.
/// Names should start with "--" (e.g. "--mando-trim-color"). Values live in
/// <see cref="SiteTheme.CustomValuesJson"/> keyed by name.
/// </summary>
public class CustomThemeVariable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
