using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OrdoWiki.Web.Components.Layout;

public partial class NavBar
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private bool _drawerOpen;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task SignOutAsync() =>
        await JS.InvokeVoidAsync("ordoAuth.submitLogout");
}
