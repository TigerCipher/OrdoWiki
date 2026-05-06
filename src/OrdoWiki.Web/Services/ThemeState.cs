namespace OrdoWiki.Web.Services;

public class ThemeState
{
    public bool IsDarkMode
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            Changed?.Invoke();
        }
    } = true;

    public event Action? Changed;

    public void Toggle() => IsDarkMode = !IsDarkMode;
}
