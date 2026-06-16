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

    public void Toggle() => IsDarkMode = !IsDarkMode;

    public void Set(bool isDark) => IsDarkMode = isDark;
}
