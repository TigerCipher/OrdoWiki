namespace OrdoWiki.Web.Components.Pages.Design;

using Data.Auth;
using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OrdoWiki.Web.Components.Layout;
using OrdoWiki.Web.Models.Requests;
using OrdoWiki.Web.Services;

public partial class ThemePage
{
    private SiteThemeDto _theme = new();
    private List<CustomThemeVariableDto> _customVariables = [];
    private MudTheme _defaults = OrdoTheme.Build();
    private bool _loading = true;
    private bool _saving;
    private bool _uploading;
    private bool _isAdmin;

    private string _newVarName = string.Empty;
    private string _newVarDescription = string.Empty;

    [Inject]
    private ISiteThemeService SiteThemeService { get; set; } = null!;

    [Inject]
    private IMediaService MediaService { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthProvider { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive) return;

        AuthenticationState auth = await AuthProvider.GetAuthenticationStateAsync();
        _isAdmin = auth.User.IsInRole(Roles.Admin);

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _theme = await SiteThemeService.GetAsync();
        _customVariables = (await SiteThemeService.GetCustomVariablesAsync()).ToList();
        _loading = false;
    }

    // --- Palette accessors ---

    private string GetLightValue(string key) =>
        _theme.LightPalette.TryGetValue(key, out string? v) && !string.IsNullOrEmpty(v)
            ? v
            : OrdoTheme.ReadPaletteValue(_defaults.PaletteLight, key) ?? "#000000";

    private string GetDarkValue(string key) =>
        _theme.DarkPalette.TryGetValue(key, out string? v) && !string.IsNullOrEmpty(v)
            ? v
            : OrdoTheme.ReadPaletteValue(_defaults.PaletteDark, key) ?? "#000000";

    private void SetLightValue(string key, string value) => _theme.LightPalette[key] = value;
    private void SetDarkValue(string key, string value) => _theme.DarkPalette[key] = value;

    // --- Custom variable accessors ---

    private string GetCustomValue(string name, bool isDark)
    {
        if (!_theme.CustomValues.TryGetValue(name, out ThemeValuePair? pair) || pair is null)
            return "#888888";
        string? val = isDark ? pair.Dark : pair.Light;
        return string.IsNullOrEmpty(val) ? "#888888" : val;
    }

    private void SetCustomValue(string name, bool isDark, string value)
    {
        if (!_theme.CustomValues.TryGetValue(name, out ThemeValuePair? pair) || pair is null)
        {
            pair = new ThemeValuePair();
            _theme.CustomValues[name] = pair;
        }
        if (isDark) pair.Dark = value;
        else pair.Light = value;
    }

    // --- Background uploads ---

    private async Task UploadBackgroundAsync(InputFileChangeEventArgs e, bool isDark)
    {
        if (e.FileCount == 0) return;
        IBrowserFile picked = e.File;
        if (picked.Size > MediaLimits.MaxImageBytes)
        {
            Snackbar.Add($"Image is too large (max {MediaLimits.MaxImageBytes / (1024 * 1024)} MB).", Severity.Warning);
            return;
        }

        _uploading = true;
        try
        {
            await using Stream stream = picked.OpenReadStream(MediaLimits.MaxImageBytes);
            ApiResponse<MediaAssetDto> response = await MediaService.UploadImageAsync(
                stream, picked.Name, picked.ContentType, picked.Size,
                MediaSourceType.Banner);

            if (!response)
            {
                Snackbar.Add($"Upload failed: {response.Error}", Severity.Error);
                return;
            }

            if (isDark)
            {
                _theme.DarkBackgroundAssetId = response.Value.Id;
                _theme.DarkBackgroundUrl = response.Value.StoragePath;
            }
            else
            {
                _theme.LightBackgroundAssetId = response.Value.Id;
                _theme.LightBackgroundUrl = response.Value.StoragePath;
            }
        }
        finally
        {
            _uploading = false;
        }
    }

    private void ClearBackground(bool isDark)
    {
        if (isDark)
        {
            _theme.DarkBackgroundAssetId = null;
            _theme.DarkBackgroundUrl = null;
        }
        else
        {
            _theme.LightBackgroundAssetId = null;
            _theme.LightBackgroundUrl = null;
        }
    }

    // --- Save ---

    private async Task SaveAsync()
    {
        _saving = true;
        try
        {
            ApiResponse<SiteThemeDto> response = await SiteThemeService.SaveAsync(new SaveSiteThemeRequest
            {
                LightPalette = _theme.LightPalette,
                DarkPalette = _theme.DarkPalette,
                CustomValues = _theme.CustomValues,
                LightBackgroundAssetId = _theme.LightBackgroundAssetId,
                DarkBackgroundAssetId = _theme.DarkBackgroundAssetId,
            });

            if (!response.Success)
            {
                Snackbar.Add($"Save failed: {response.Error}", Severity.Error);
                return;
            }

            _theme = response.Value;
            Snackbar.Add("Theme saved.", Severity.Success);
        }
        finally
        {
            _saving = false;
        }
    }

    // --- Custom variable management ---

    private async Task AddCustomVariableAsync()
    {
        if (string.IsNullOrWhiteSpace(_newVarName))
        {
            Snackbar.Add("Variable name is required.", Severity.Warning);
            return;
        }

        ApiResponse<CustomThemeVariableDto> response = await SiteThemeService.CreateCustomVariableAsync(
            new CreateCustomVariableRequest
            {
                Name = _newVarName,
                Description = _newVarDescription,
                SortOrder = _customVariables.Count,
            });

        if (!response.Success)
        {
            Snackbar.Add($"Failed to add: {response.Error}", Severity.Error);
            return;
        }

        _customVariables.Add(response.Value);
        _newVarName = string.Empty;
        _newVarDescription = string.Empty;
        Snackbar.Add("Variable added.", Severity.Success);
    }

    private async Task DeleteCustomVariableAsync(CustomThemeVariableDto v)
    {
        ApiResponse<bool> response = await SiteThemeService.DeleteCustomVariableAsync(v.Id);
        if (!response.Success)
        {
            Snackbar.Add($"Failed to delete: {response.Error}", Severity.Error);
            return;
        }

        _customVariables.Remove(v);
        _theme.CustomValues.Remove(v.Name);
        Snackbar.Add("Variable removed.", Severity.Success);
    }
}
