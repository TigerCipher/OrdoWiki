namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class NavBar
{
    private bool _drawerOpen;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task SignOutAsync() =>
        await JS.InvokeVoidAsync("ordoAuth.submitLogout");
}