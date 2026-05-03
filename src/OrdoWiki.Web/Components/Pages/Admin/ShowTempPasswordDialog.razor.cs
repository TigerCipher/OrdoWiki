using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace OrdoWiki.Web.Components.Pages.Admin;

public partial class ShowTempPasswordDialog
{
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public string Username { get; set; } = "";
    [Parameter, EditorRequired] public string TempPassword { get; set; } = "";

    private async Task Copy()
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", TempPassword);
        Snackbar.Add("Copied to clipboard.", Severity.Success);
    }

    private void Close() => MudDialog.Close(DialogResult.Ok(true));
}
