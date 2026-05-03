namespace OrdoWiki.Web.Components.Pages.Admin;

using Data.Auth;
using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Models;
using MudBlazor;

public partial class InvitesTab
{
    [Inject]
    private InviteCodeService InviteCodes { get; set; } = default!;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private IDialogService Dialog { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthState { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private bool Loading { get; set; } = true;
    private List<InviteRow> Rows { get; set; } = [];
    private string CurrentUserId { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState auth = await AuthState.GetAuthenticationStateAsync();
        CurrentUserId = UserManager.GetUserId(auth.User) ?? "";
        await Reload();
    }

    private async Task Reload()
    {
        Loading = true;
        StateHasChanged();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<InviteCode> invites = await InviteCodes.Query().OrderByDescending(i => i.CreatedAt).ToListAsync();

        List<string> creatorIds = invites.Select(i => i.CreatedByUserId).Distinct().ToList();
        Dictionary<string, string> creators = await UserManager.Users
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Id);

        Rows = invites.Select(i => new InviteRow(
            i.Id,
            i.Code,
            i.AssignedRole,
            GetStatus(i, now),
            creators.GetValueOrDefault(i.CreatedByUserId, i.CreatedByUserId),
            i.ExpiresAt)).ToList();

        Loading = false;
        StateHasChanged();
    }

    private async Task Generate()
    {
        IDialogReference dialog = await Dialog.ShowAsync<GenerateInviteDialog>("Generate invite");
        DialogResult? result = await dialog.Result;
        if (result is { Canceled: false, Data: GenerateInviteResult r })
        {
            try
            {
                InviteCode created = await InviteCodes.GenerateAsync(r.Role, CurrentUserId, r.ExpiresInDays);
                string link = $"{Nav.BaseUri.TrimEnd('/')}/Account/Register?code={created.Code}";
                await Js.InvokeVoidAsync("navigator.clipboard.writeText", link);
                Snackbar.Add("Invite generated and link copied to clipboard.", Severity.Success);
                await Reload();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to generate: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task CopyLink(InviteRow row)
    {
        string link = $"{Nav.BaseUri.TrimEnd('/')}/Account/Register?code={row.Code}";
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", link);
        Snackbar.Add("Invite link copied.", Severity.Success);
    }

    private async Task Revoke(InviteRow row)
    {
        bool? confirm = await Dialog.ShowMessageBoxAsync(
            "Revoke invite",
            $"Revoke code {row.Code}? It will stop working immediately.",
            "Revoke", cancelText: "Cancel");
        if (confirm != true) return;

        bool ok = await InviteCodes.RevokeAsync(row.Id, CurrentUserId);
        if (ok)
        {
            Snackbar.Add("Invite revoked.", Severity.Success);
            await Reload();
        }
        else
            Snackbar.Add("Already redeemed or revoked.", Severity.Warning);
    }

    private static InviteStatus GetStatus(InviteCode i, DateTimeOffset now)
    {
        if (i.IsRevoked) return InviteStatus.Revoked;
        if (i.IsRedeemed) return InviteStatus.Redeemed;
        if (i.IsExpired(now)) return InviteStatus.Expired;
        return InviteStatus.Active;
    }
}