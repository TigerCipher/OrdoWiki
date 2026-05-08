namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Services;

public partial class NavBar
{
    private bool _drawerOpen;
    private MudMenu? _desktopMenu;
    private MudMenu? _mobileMenu;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ThemeState ThemeState { get; set; } = null!;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task SignOutAsync() =>
        await JsRuntime.InvokeVoidAsync("ordoAuth.submitLogout");

    private void ToggleTheme() => ThemeState.Toggle();

    private Task ToggleDesktopMenuAsync(MouseEventArgs e) =>
        _desktopMenu?.ToggleMenuAsync(e) ?? Task.CompletedTask;

    private Task ToggleMobileMenuAsync(MouseEventArgs e) =>
        _mobileMenu?.ToggleMenuAsync(e) ?? Task.CompletedTask;
}
