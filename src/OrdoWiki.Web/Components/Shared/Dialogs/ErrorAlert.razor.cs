namespace OrdoWiki.Web.Components.Shared.Dialogs;

using Microsoft.AspNetCore.Components;
using MudBlazor;

public partial class ErrorAlert
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;
    
    [Parameter]
    public string ErrorMessage { get; set; } = string.Empty;
    
    private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
}