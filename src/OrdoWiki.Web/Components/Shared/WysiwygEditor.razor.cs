namespace OrdoWiki.Web.Components.Shared;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public partial class WysiwygEditor : IAsyncDisposable
{
    private readonly string _editorId = $"wysiwyg-{Guid.NewGuid():N}";
    private DotNetObjectReference<WysiwygEditor>? _selfRef;
    private string _lastPushed = string.Empty;
    private bool _initialized;

    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public string Label { get; set; } = "Content";

    [Parameter]
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Standalone;

    [Parameter]
    public Guid? SourceId { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override void OnParametersSet()
    {
        // Track the value we last handed to the editor so external resets (e.g. a
        // load-from-server) can push new content in without echoing our own updates
        // back through OnContentChangedAsync -> parent state -> OnParametersSet.
        if (!_initialized) _lastPushed = Value ?? string.Empty;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("ordoWysiwyg.init", _editorId, Value ?? string.Empty, _selfRef);
            _initialized = true;
            _lastPushed = Value ?? string.Empty;
            return;
        }

        if (!_initialized) return;

        string incoming = Value ?? string.Empty;
        if (incoming == _lastPushed) return;

        _lastPushed = incoming;
        await JsRuntime.InvokeVoidAsync("ordoWysiwyg.setContent", _editorId, incoming);
    }

    [JSInvokable]
    public async Task OnContentChangedAsync(string html)
    {
        _lastPushed = html;
        await ValueChanged.InvokeAsync(html);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("ordoWysiwyg.destroy", _editorId);
        }
        catch (JSDisconnectedException)
        {
            // Circuit is already gone; nothing to clean up.
        }
        catch (JSException)
        {
        }

        _selfRef?.Dispose();
    }
}
