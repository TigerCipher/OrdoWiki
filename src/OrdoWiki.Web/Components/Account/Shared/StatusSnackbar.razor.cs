using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OrdoWiki.Web.Components.Account.Shared;

public partial class StatusSnackbar
{
    public const string CookieName = "OrdoWiki.PasswordChanged";
    private const string StateKey = "password-changed-snackbar";
    private const string ErrorPrefix = "Error:";

    private string? _message;
    private PersistingComponentStateSubscription _subscription;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    protected override void OnInitialized()
    {
        if (HttpContext is not null)
        {
            _message = HttpContext.Request.Cookies[CookieName];
            if (_message is not null)
            {
                HttpContext.Response.Cookies.Delete(CookieName);
            }
        }
        else if (ApplicationState.TryTakeFromJson<string>(StateKey, out string? persisted))
        {
            _message = persisted;
        }

        _subscription = ApplicationState.RegisterOnPersisting(() =>
        {
            if (_message is not null)
            {
                ApplicationState.PersistAsJson(StateKey, _message);
            }
            return Task.CompletedTask;
        });
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (HttpContext is not null || _message is null)
        {
            return;
        }

        bool isError = _message.StartsWith(ErrorPrefix, StringComparison.OrdinalIgnoreCase);
        string text = isError ? _message[ErrorPrefix.Length..].Trim() : _message;
        Snackbar.Add(text, isError ? Severity.Error : Severity.Success);
        _message = null;
    }

    public void Dispose() => _subscription.Dispose();
}
