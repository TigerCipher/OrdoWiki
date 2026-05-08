namespace OrdoWiki.Web.Services;

public class ThemeState
{
    public event Action? Changed;
    
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
    

    public void Toggle() => IsDarkMode = !IsDarkMode;
}
