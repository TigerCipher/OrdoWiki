namespace OrdoWiki.Web.Services;

/// <summary>
/// Scoped notification channel so the carousel can re-fetch banners after a Designer/Admin
/// edits them — without polling or forcing a full page reload.
/// </summary>
public class BannerState
{
    public event Func<Task>? Changed;

    public Task NotifyChangedAsync() => Changed?.Invoke() ?? Task.CompletedTask;
}
