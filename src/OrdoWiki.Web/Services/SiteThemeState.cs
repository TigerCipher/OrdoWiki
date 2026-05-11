namespace OrdoWiki.Web.Services;

/// <summary>
/// Notification channel used by ThemeHost to refresh after a Designer/Admin saves the theme.
/// Same pattern as <see cref="BannerState"/>.
/// </summary>
public class SiteThemeState
{
    public event Func<Task>? Changed;

    public Task NotifyChangedAsync() => Changed?.Invoke() ?? Task.CompletedTask;
}
