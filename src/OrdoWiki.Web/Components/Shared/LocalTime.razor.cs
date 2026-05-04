namespace OrdoWiki.Web.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class LocalTime
{

    private string _formatted = string.Empty;
    
    [Parameter, EditorRequired]
    public DateTime DateTime { get; set; }

    [Parameter]
    public string? Mode { get; set; } = "datetime"; // datetime | date | time | relative
    
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override void OnParametersSet() => _formatted = DateTime.ToString("u");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(!firstRender) return;
        _formatted = await JsRuntime.InvokeAsync<string>("ordoTime.format", DateTime, Mode);
        StateHasChanged();
    }
}