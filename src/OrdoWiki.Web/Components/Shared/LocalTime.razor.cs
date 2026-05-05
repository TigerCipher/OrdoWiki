namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class LocalTime
{
    private string _formatted = string.Empty;
    private DateTime? _lastFormatted;

    [Parameter, EditorRequired]
    public DateTime DateTime { get; set; }

    [Parameter]
    public string? Mode { get; set; } = "datetime"; // datetime | date | time | relative

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override void OnParametersSet()
    {
        // SSR / pre-render fallback. Replaced by JS-formatted local time once interactive.
        if (string.IsNullOrEmpty(_formatted) || _lastFormatted != DateTime)
        {
            _formatted = AsUtc(DateTime).ToString("yyyy-MM-dd HH:mm 'UTC'");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Re-run whenever the DateTime parameter changes, not only on the first render —
        // otherwise a parent that loads data after the first render leaves us stuck on the SSR fallback.
        if (_lastFormatted == DateTime) return;
        _lastFormatted = DateTime;

        // DateTime.MinValue is the initial-state value before the parent has loaded data.
        // Don't burn a JS round-trip on a placeholder.
        if (DateTime == default) return;

        string utcIso = AsUtc(DateTime).ToString("o");
        string formatted = await JsRuntime.InvokeAsync<string>("ordoTime.format", utcIso, Mode);

        if (_formatted == formatted) return;
        _formatted = formatted;
        StateHasChanged();
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
