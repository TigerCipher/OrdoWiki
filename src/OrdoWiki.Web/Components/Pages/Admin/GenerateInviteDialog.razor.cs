using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrdoWiki.Data.Auth;
using OrdoWiki.Web.Components.Models;

namespace OrdoWiki.Web.Components.Pages.Admin;

public partial class GenerateInviteDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    private string Role { get; set; } = OrdoWiki.Data.Auth.Roles.Editor;
    private int ExpiresInDays { get; set; } = InviteCodeService.DefaultExpirationDays;

    private void Confirm() => MudDialog.Close(DialogResult.Ok(new GenerateInviteResult(Role, ExpiresInDays)));
    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}
