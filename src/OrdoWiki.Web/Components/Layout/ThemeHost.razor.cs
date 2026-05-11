namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrdoWiki.Web.Models;
using OrdoWiki.Web.Services;
using System.Text;

public partial class ThemeHost : IDisposable
{
    private MudTheme _theme = OrdoTheme.Build();
    private SiteThemeDto _siteTheme = new();
    private string _customCss = string.Empty;

    [Inject]
    private ThemeState ThemeState { get; set; } = null!;

    [Inject]
    private SiteThemeState SiteThemeState { get; set; } = null!;

    [Inject]
    private ISiteThemeService SiteThemeService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        ThemeState.Changed += OnThemeStateChanged;
        SiteThemeState.Changed += RefreshSiteThemeAsync;
        await RefreshSiteThemeAsync();
    }

    private async Task RefreshSiteThemeAsync()
    {
        _siteTheme = await SiteThemeService.GetAsync();
        _theme = OrdoTheme.Build(_siteTheme);
        RebuildCustomCss();
        await InvokeAsync(StateHasChanged);
    }

    private void OnThemeStateChanged()
    {
        // Re-emit per-mode CSS vars + background when the user toggles dark/light.
        RebuildCustomCss();
        InvokeAsync(StateHasChanged);
    }

    private void RebuildCustomCss()
    {
        bool dark = ThemeState.IsDarkMode;
        StringBuilder sb = new();

        sb.AppendLine(":root {");
        foreach ((string name, ThemeValuePair pair) in _siteTheme.CustomValues)
        {
            string? val = dark ? pair.Dark : pair.Light;
            if (string.IsNullOrWhiteSpace(val)) continue;
            sb.Append("  ").Append(name).Append(": ").Append(val).AppendLine(";");
        }
        sb.AppendLine("}");

        string? bg = dark ? _siteTheme.DarkBackgroundUrl : _siteTheme.LightBackgroundUrl;
        if (!string.IsNullOrEmpty(bg))
        {
            sb.AppendLine("body {");
            sb.Append("  background-image: url('").Append(bg).AppendLine("');");
            sb.AppendLine("  background-size: cover;");
            sb.AppendLine("  background-position: center;");
            sb.AppendLine("  background-attachment: fixed;");
            sb.AppendLine("}");
        }

        _customCss = sb.ToString();
    }

    public void Dispose()
    {
        ThemeState.Changed -= OnThemeStateChanged;
        SiteThemeState.Changed -= RefreshSiteThemeAsync;
        GC.SuppressFinalize(this);
    }
}
