namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OrdoWiki.Web.Models;
using OrdoWiki.Web.Services;
using System.Text;

public partial class ThemeHost : IDisposable
{
    private const string StorageKey = "ordoTheme";

    private MudTheme _theme = OrdoTheme.Build();
    private SiteThemeDto _siteTheme = new();
    private string _customCss = string.Empty;
    private bool _storageLoaded;

    [Inject]
    private ThemeState ThemeState { get; set; } = null!;

    [Inject]
    private SiteThemeState SiteThemeState { get; set; } = null!;

    [Inject]
    private ISiteThemeService SiteThemeService { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        ThemeState.Changed += OnThemeStateChanged;
        SiteThemeState.Changed += RefreshSiteThemeAsync;
        await RefreshSiteThemeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _storageLoaded) return;
        _storageLoaded = true;

        try
        {
            string? stored = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            bool? desired = stored switch
            {
                "dark" => true,
                "light" => false,
                _ => null,
            };

            if (desired is { } d && d != ThemeState.IsDarkMode)
                ThemeState.Set(d);
        }
        catch
        {
            // JS not ready or localStorage unavailable — keep the default.
        }
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
        _ = PersistThemeAsync();
    }

    private async Task PersistThemeAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync(
                "localStorage.setItem", StorageKey, ThemeState.IsDarkMode ? "dark" : "light");
        }
        catch
        {
            // Ignore — write-best-effort.
        }
    }

    private void RebuildCustomCss()
    {
        bool dark = ThemeState.IsDarkMode;
        StringBuilder sb = new();

        // Diagnostic header so the live mode + URL choice is visible in dev tools.
        sb.Append("/* ordo-theme: mode=").Append(dark ? "dark" : "light")
          .Append(" lightUrl=").Append(_siteTheme.LightBackgroundUrl ?? "<none>")
          .Append(" darkUrl=").Append(_siteTheme.DarkBackgroundUrl ?? "<none>")
          .AppendLine(" */");

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
            // Target html (not body) so the image always covers the full viewport
            // even on short pages — `background-attachment: fixed` is still clipped
            // to the element's box, and body can be shorter than 100vh.
            sb.AppendLine("html {");
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
