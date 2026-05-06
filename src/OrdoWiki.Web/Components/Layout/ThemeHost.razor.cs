namespace OrdoWiki.Web.Components.Layout;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using Services;

public partial class ThemeHost : IDisposable
{
    private readonly MudTheme _theme = OrdoTheme.Build();

    [Inject]
    private ThemeState ThemeState { get; set; } = null!;

    protected override void OnInitialized()
    {
        ThemeState.Changed += OnThemeChanged;
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        ThemeState.Changed -= OnThemeChanged;
        GC.SuppressFinalize(this);
    }
}
