namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Services;

public partial class NavBar
{
    private bool _drawerOpen;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ThemeState ThemeState { get; set; } = null!;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task SignOutAsync() =>
        await JsRuntime.InvokeVoidAsync("ordoAuth.submitLogout");

    private void ToggleTheme() => ThemeState.Toggle();
}
