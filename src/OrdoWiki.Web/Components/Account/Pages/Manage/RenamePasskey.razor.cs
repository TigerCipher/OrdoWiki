namespace OrdoWiki.Web.Components.Account.Pages.Manage;

using System.Buffers.Text;
using Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

public partial class RenamePasskey
{
    [Parameter]
    public string? Id { get; set; }

    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private UserPasskeyInfo? CurrentPasskey { get; set; }
    private string Name { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is null || string.IsNullOrEmpty(Id)) return;

        ApplicationUser? user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null) return;

        byte[] credentialId;
        try
        {
            credentialId = Base64Url.DecodeFromChars(Id);
        }
        catch (FormatException)
        {
            return;
        }

        CurrentPasskey = await UserManager.GetPasskeyAsync(user, credentialId);
        Name = CurrentPasskey?.Name ?? "";
    }
}