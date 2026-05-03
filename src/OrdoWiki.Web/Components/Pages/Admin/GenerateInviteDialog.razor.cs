namespace OrdoWiki.Web.Components.Pages.Admin;

using Data.Auth;
using Microsoft.AspNetCore.Components;
using Models;
using MudBlazor;

public partial class GenerateInviteDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private string Role { get; set; } = Roles.Editor;
    private int ExpiresInDays { get; set; } = InviteCodeService.DefaultExpirationDays;

    private void Confirm() => MudDialog.Close(DialogResult.Ok(new GenerateInviteResult(Role, ExpiresInDays)));
    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}