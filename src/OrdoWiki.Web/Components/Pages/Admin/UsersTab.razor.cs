namespace OrdoWiki.Web.Components.Pages.Admin;

using System.Security.Cryptography;
using Data;
using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using MudBlazor;

public partial class UsersTab
{
    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private ApplicationDbContext Db { get; set; } = default!;

    [Inject]
    private IDialogService Dialog { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthState { get; set; } = default!;

    private bool Loading { get; set; } = true;
    private List<UserRow> Users { get; set; } = [];
    private string? CurrentUserId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState auth = await AuthState.GetAuthenticationStateAsync();
        CurrentUserId = UserManager.GetUserId(auth.User);
        await Reload();
    }

    private async Task Reload()
    {
        Loading = true;
        StateHasChanged();

        List<ApplicationUser> users = await Db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        List<UserRow> rows = new(users.Count);
        foreach (ApplicationUser u in users)
        {
            IList<string> roles = await UserManager.GetRolesAsync(u);
            rows.Add(new UserRow(
                u.Id,
                u.UserName ?? "",
                u.DisplayName ?? "",
                roles,
                u.LockoutEnd is { } end && end > DateTimeOffset.UtcNow,
                u.IsPasswordResetRequired));
        }

        Users = rows;
        Loading = false;
        StateHasChanged();
    }

    private async Task EditRoles(UserRow row)
    {
        DialogParameters<EditRolesDialog> parameters = new()
        {
            { x => x.UserId, row.Id },
            { x => x.Username, row.Username },
            { x => x.CurrentRoles, row.Roles.ToList() }
        };
        IDialogReference dialog = await Dialog.ShowAsync<EditRolesDialog>("Edit roles", parameters);
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false }) await Reload();
    }

    private async Task Lock(UserRow row)
    {
        ApplicationUser? u = await UserManager.FindByIdAsync(row.Id);
        if (u is null) return;
        await UserManager.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);
        Snackbar.Add($"Locked {row.Username}.", Severity.Success);
        await Reload();
    }

    private async Task Unlock(UserRow row)
    {
        ApplicationUser? u = await UserManager.FindByIdAsync(row.Id);
        if (u is null) return;
        await UserManager.SetLockoutEndDateAsync(u, null);
        Snackbar.Add($"Unlocked {row.Username}.", Severity.Success);
        await Reload();
    }

    private async Task ResetPassword(UserRow row)
    {
        ApplicationUser? u = await UserManager.FindByIdAsync(row.Id);
        if (u is null) return;

        string tempPassword = GenerateTempPassword();
        string token = await UserManager.GeneratePasswordResetTokenAsync(u);
        IdentityResult result = await UserManager.ResetPasswordAsync(u, token, tempPassword);
        if (!result.Succeeded)
        {
            Snackbar.Add($"Reset failed: {string.Join(", ", result.Errors.Select(e => e.Description))}",
                Severity.Error);
            return;
        }

        u.IsPasswordResetRequired = true;
        await UserManager.UpdateAsync(u);
        await UserManager.UpdateSecurityStampAsync(u);

        DialogParameters<ShowTempPasswordDialog> parameters = new()
        {
            { x => x.Username, row.Username },
            { x => x.TempPassword, tempPassword }
        };
        await Dialog.ShowAsync<ShowTempPasswordDialog>("Temporary password", parameters);
        await Reload();
    }

    private async Task Delete(UserRow row)
    {
        if (row.Id == CurrentUserId)
        {
            Snackbar.Add("You can't delete yourself.", Severity.Warning);
            return;
        }

        bool? confirm = await Dialog.ShowMessageBoxAsync(
            "Delete user",
            $"Permanently delete {row.Username}? This cannot be undone.",
            "Delete", cancelText: "Cancel");
        if (confirm != true) return;

        ApplicationUser? u = await UserManager.FindByIdAsync(row.Id);
        if (u is null) return;
        IdentityResult result = await UserManager.DeleteAsync(u);
        if (!result.Succeeded)
        {
            Snackbar.Add($"Delete failed: {string.Join(", ", result.Errors.Select(e => e.Description))}",
                Severity.Error);
            return;
        }

        Snackbar.Add($"Deleted {row.Username}.", Severity.Success);
        await Reload();
    }

    private static string GenerateTempPassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        const string symbols = "!@#$%^&*";
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        char[] result = new char[14];
        for (int i = 0; i < 12; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        result[12] = (char)('0' + bytes[0] % 10);
        result[13] = symbols[bytes[1] % symbols.Length];
        return new string(result);
    }
}