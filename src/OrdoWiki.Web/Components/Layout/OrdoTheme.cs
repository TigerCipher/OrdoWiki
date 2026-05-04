namespace OrdoWiki.Web.Components.Layout;

using MudBlazor;

public static class OrdoTheme
{
    public static MudTheme Build() => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#c8a560",   // beskar gold
            Secondary = "#8a7a5a", // muted brass
            Tertiary = "#9a3a2a",  // Mando red
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
            TableHover = "#ffffff55"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["'Inter'", "'Segoe UI'", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1rem",
                LineHeight = "1.55"
            },
            H1 = new H1Typography { FontSize = "2.5rem", FontWeight = "600" },
            H2 = new H2Typography { FontSize = "2rem", FontWeight = "600" },
            H3 = new H3Typography { FontSize = "1.5rem", FontWeight = "600" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
            AppbarHeight = "64px",
            DrawerWidthLeft = "260px"
        }
    };
}