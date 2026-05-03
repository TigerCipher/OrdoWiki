namespace OrdoWiki.Web.Components.Shared.Dialogs;

using MudBlazor;

public interface IOrdoDialogs
{
    Task<IDialogReference> ShowErrorAsync(string message);
}