using System.Buffers.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using OrdoWiki.Data.Entities;

namespace OrdoWiki.Web.Components.Account.Pages.Manage;

public partial class RenamePasskey
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    [Parameter] public string? Id { get; set; }

    private UserPasskeyInfo? CurrentPasskey { get; set; }
    private string Name { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is null || string.IsNullOrEmpty(Id))
        {
            return;
        }

        ApplicationUser? user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            return;
        }

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
