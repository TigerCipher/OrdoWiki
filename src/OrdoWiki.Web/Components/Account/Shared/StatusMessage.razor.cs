namespace OrdoWiki.Web.Components.Account.Shared;

using Microsoft.AspNetCore.Components;

public partial class StatusMessage
{
    private const string StateKey = "status-message";

    private string? _messageFromCookie;
    private PersistingComponentStateSubscription _subscription;

    [Parameter]
    public string? Message { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? DisplayMessage => Message ?? _messageFromCookie;

    protected override void OnInitialized()
    {
        if (HttpContext is not null)
        {
            _messageFromCookie = HttpContext.Request.Cookies[IdentityRedirectManager.StatusCookieName];
            if (_messageFromCookie is not null)
            {
                HttpContext.Response.Cookies.Delete(IdentityRedirectManager.StatusCookieName);
            }
        }
        else
        {
            if (ApplicationState.TryTakeFromJson<string>(StateKey, out string? persisted))
            {
                _messageFromCookie = persisted;
            }
        }

        _subscription = ApplicationState.RegisterOnPersisting(() =>
        {
            if (_messageFromCookie is not null)
            {
                ApplicationState.PersistAsJson(StateKey, _messageFromCookie);
            }
            return Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        _subscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
