namespace OrdoWiki.Web.Components.Account.Pages.Manage;

using System.Buffers.Text;
using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

public partial class Passkeys
{
    public const int MaxPasskeyCount = 100;

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [Inject]
    private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;

    [Inject]
    private IdentityRedirectManager RedirectManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private string? Action { get; set; }

    [SupplyParameterFromForm]
    private string? CredentialId { get; set; }

    [SupplyParameterFromForm(FormName = "add-passkey")]
    private PasskeyInputModel Input { get; set; } = default!;

    private ApplicationUser? CurrentUser { get; set; }
    private IList<UserPasskeyInfo>? CurrentPasskeys { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Input ??= new PasskeyInputModel();

        CurrentUser = await UserManager.GetUserAsync(HttpContext.User);
        if (CurrentUser is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        CurrentPasskeys = await UserManager.GetPasskeysAsync(CurrentUser);
    }

    private async Task AddPasskey()
    {
        if (CurrentUser is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        if (!string.IsNullOrEmpty(Input.Error))
        {
            RedirectManager.RedirectToCurrentPageWithStatus($"Error: {Input.Error}", HttpContext);
            return;
        }

        if (string.IsNullOrEmpty(Input.CredentialJson))
        {
            RedirectManager.RedirectToCurrentPageWithStatus("Error: The browser did not provide a passkey.",
                HttpContext);
            return;
        }

        if (CurrentPasskeys!.Count >= MaxPasskeyCount)
        {
            RedirectManager.RedirectToCurrentPageWithStatus("Error: Maximum passkeys reached.", HttpContext);
            return;
        }

        PasskeyAttestationResult attestationResult =
            await SignInManager.PerformPasskeyAttestationAsync(Input.CredentialJson);
        if (!attestationResult.Succeeded)
        {
            RedirectManager.RedirectToCurrentPageWithStatus($"Error: {attestationResult.Failure.Message}", HttpContext);
            return;
        }

        IdentityResult addResult = await UserManager.AddOrUpdatePasskeyAsync(CurrentUser, attestationResult.Passkey);
        if (!addResult.Succeeded)
        {
            RedirectManager.RedirectToCurrentPageWithStatus("Error: The passkey could not be added.", HttpContext);
            return;
        }

        string id = Base64Url.EncodeToString(attestationResult.Passkey.CredentialId);
        RedirectManager.RedirectTo($"Account/Manage/RenamePasskey/{id}");
    }

    private async Task UpdatePasskey()
    {
        switch (Action)
        {
            case "rename":
                RedirectManager.RedirectTo($"Account/Manage/RenamePasskey/{CredentialId}");
                break;
            case "delete":
                await DeletePasskey();
                break;
            default:
                RedirectManager.RedirectToCurrentPageWithStatus($"Error: Unknown action '{Action}'.", HttpContext);
                break;
        }
    }

    private async Task DeletePasskey()
    {
        if (CurrentUser is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        byte[] credentialId;
        try
        {
            credentialId = Base64Url.DecodeFromChars(CredentialId);
        }
        catch (FormatException)
        {
            RedirectManager.RedirectToCurrentPageWithStatus("Error: Invalid passkey ID.", HttpContext);
            return;
        }

        IdentityResult result = await UserManager.RemovePasskeyAsync(CurrentUser, credentialId);
        if (!result.Succeeded)
        {
            RedirectManager.RedirectToCurrentPageWithStatus("Error: The passkey could not be deleted.", HttpContext);
            return;
        }

        RedirectManager.RedirectToCurrentPageWithStatus("Passkey deleted.", HttpContext);
    }
}