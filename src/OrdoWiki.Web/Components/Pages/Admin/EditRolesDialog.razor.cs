namespace OrdoWiki.Web.Components.Pages.Admin;

using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

public partial class EditRolesDialog
{
    [Parameter]
    [EditorRequired]
    public string UserId { get; set; } = "";

    [Parameter]
    [EditorRequired]
    public string Username { get; set; } = "";

    [Parameter]
    [EditorRequired]
    public List<string> CurrentRoles { get; set; } = [];

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private HashSet<string> SelectedRoles { get; set; } = [];

    protected override void OnInitialized()
    {
        SelectedRoles = new HashSet<string>(CurrentRoles);
    }

    private void Toggle(string role, bool isOn)
    {
        if (isOn) SelectedRoles.Add(role);
        else SelectedRoles.Remove(role);
    }

    private async Task Save()
    {
        ApplicationUser? user = await UserManager.FindByIdAsync(UserId);
        if (user is null)
        {
            Snackbar.Add("User not found.", Severity.Error);
            MudDialog.Close(DialogResult.Cancel());
            return;
        }

        IList<string> existing = await UserManager.GetRolesAsync(user);
        IEnumerable<string> toAdd = SelectedRoles.Except(existing);
        IEnumerable<string> toRemove = existing.Except(SelectedRoles);

        IdentityResult addResult = await UserManager.AddToRolesAsync(user, toAdd);
        if (!addResult.Succeeded)
        {
            Snackbar.Add($"Add failed: {string.Join(", ", addResult.Errors.Select(e => e.Description))}",
                Severity.Error);
            return;
        }

        IdentityResult removeResult = await UserManager.RemoveFromRolesAsync(user, toRemove);
        if (!removeResult.Succeeded)
        {
            Snackbar.Add($"Remove failed: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}",
                Severity.Error);
            return;
        }

        await UserManager.UpdateSecurityStampAsync(user);
        Snackbar.Add($"Roles updated for {Username}.", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());
}