namespace OrdoWiki.Web.Components.Layout;

using MudBlazor;
using MudBlazor.Utilities;
using OrdoWiki.Web.Models;
using System.Reflection;

public static class OrdoTheme
{
    /// <summary>
    /// Subset of <see cref="Palette"/> properties exposed in the theme editor. Each entry
    /// maps to a property name on the palette object — order is the display order in the UI.
    /// </summary>
    public static readonly string[] EditablePaletteKeys =
    [
        "Primary", "Secondary", "Tertiary",
        "Info", "Success", "Warning", "Error",
        "Background", "Surface",
        "AppbarBackground", "AppbarText",
        "DrawerBackground", "DrawerText",
        "TextPrimary", "TextSecondary",
        "ActionDefault",
        "LinesDefault", "Divider",
    ];

    public static MudTheme Build(SiteThemeDto? overrides = null)
    {
        MudTheme theme = BuildDefault();
        if (overrides is not null)
        {
            ApplyPaletteOverrides(theme.PaletteLight, overrides.LightPalette);
            ApplyPaletteOverrides(theme.PaletteDark, overrides.DarkPalette);
        }
        return theme;
    }

    private static MudTheme BuildDefault() => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#c8a560",
            Secondary = "#8a7a5a",
            Tertiary = "#9a3a2a",
            Info = "#6a8caf",
            Success = "#5b8c5a",
            Warning = "#d99a3d",
            Error = "#b03a3a",

            Black = "#0a0a0a",
            White = "#f0eee9",
            Background = "#0d0d0d",
            Surface = "#1a1a1a",
            AppbarBackground = "#141414",
            AppbarText = "#e8e6e3",
            DrawerBackground = "#141414",
            DrawerText = "#e8e6e3",
            TextPrimary = "#e8e6e3",
            TextSecondary = "#9a9a9a",
            ActionDefault = "#c8a560",
            LinesDefault = "#2e2e2e",
            TableLines = "#2e2e2e",
            Divider = "#2e2e2e",
            TableHover = "#ffffff55",
        },
        PaletteLight =
        {
            Primary = "#576A56",
            Secondary = "#294832",
            Tertiary = "#78A178",
            Info = "#3D6C3C",
            Success = "#63BE61",
            Warning = "#ECBC56",
            Error = "#CC553D",
            Surface = "#F4F9F3",
            Background = "f0eee9",
            Black = "#0a0a0a",
            White = "#f0eee9",
            DrawerBackground = "#184F16",
            AppbarBackground = "#184F16",
            AppbarText = "#F4F9F3",
            DrawerText = "#F4F9F3",
            TextPrimary = "#101C13",
            TextSecondary = "#294832",
            ActionDefault = "#576A56",
            LinesDefault = "#E7DFDB",
            TableLines = "#E7DFDB",
            Divider = "#E7DFDB",
            TableHover = "#ffffff55",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["'Inter'", "'Segoe UI'", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1rem",
                LineHeight = "1.55",
            },
            H1 = new H1Typography { FontSize = "2.5rem", FontWeight = "600" },
            H2 = new H2Typography { FontSize = "2rem", FontWeight = "600" },
            H3 = new H3Typography { FontSize = "1.5rem", FontWeight = "600" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
            AppbarHeight = "64px",
            DrawerWidthLeft = "260px",
        },
    };

    /// <summary>Read the current value of a palette key as a hex string for editor display.</summary>
    public static string? ReadPaletteValue(Palette palette, string key)
    {
        PropertyInfo? prop = palette.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
        if (prop is null) return null;
        object? value = prop.GetValue(palette);
        return value switch
        {
            MudColor c => c.Value,
            string s => s,
            _ => null,
        };
    }

    private static void ApplyPaletteOverrides(Palette palette, Dictionary<string, string> overrides)
    {
        foreach ((string key, string val) in overrides)
        {
            if (string.IsNullOrWhiteSpace(val)) continue;
            PropertyInfo? prop = palette.GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite) continue;
            if (prop.PropertyType != typeof(MudColor)) continue;
            try { prop.SetValue(palette, new MudColor(val)); }
            catch { /* swallow — invalid color string from a user is a no-op, not a crash */ }
        }
    }
}
