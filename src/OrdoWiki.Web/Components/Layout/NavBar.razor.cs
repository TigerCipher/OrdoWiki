namespace OrdoWiki.Web.Components.Layout;

public partial class NavBar
{
    private bool _drawerOpen;

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
        Console.WriteLine("Toggled drawer");
    }
}

