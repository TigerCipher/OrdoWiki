namespace OrdoWiki.Web.Components.Shared.Dialogs;

using MudBlazor;

public class OrdoDialogs(IDialogService dialogService) : IOrdoDialogs
{
    public async Task<IDialogReference> ShowErrorAsync(string message)
    {
        DialogParameters parameters = new DialogParameters
        {
            { "ErrorMessage", message }
        };

        return await dialogService.ShowAsync<ErrorAlert>("Error", parameters);
    }
}