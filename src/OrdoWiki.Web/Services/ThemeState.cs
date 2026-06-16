namespace OrdoWiki.Web.Services;

public class ThemeState
{
    public event Action? Changed;

    private bool _isDarkMode = true;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        private set
        {
            if (_isDarkMode == value) return;
            _isDarkMode = value;
            Changed?.Invoke();
        }
    }

    // Diagnostic — incremented on every Toggle call so we can prove the click
    // actually reached this service (vs. the renderer not picking up the change).
    public int ToggleCount { get; private set; }

    public void Toggle()
    {
        ToggleCount++;
        IsDarkMode = !IsDarkMode;
    }

    public void Set(bool isDark) => IsDarkMode = isDark;
}
