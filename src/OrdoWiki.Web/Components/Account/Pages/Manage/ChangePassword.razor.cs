using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using OrdoWiki.Data.Entities;

namespace OrdoWiki.Web.Components.Account.Pages.Manage;

using MudBlazor;

public partial class ChangePassword
{
    [Inject]
    private UserManager<ApplicationUser> UserManager { get; set; } = null!;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = null!;

    private string OldPassword { get; set; } = "";
    private string NewPassword { get; set; } = "";
    private string ConfirmPassword { get; set; } = "";
    private bool MustReset { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is not null)
        {
            ApplicationUser? user = await UserManager.GetUserAsync(HttpContext.User);
            MustReset = user?.IsPasswordResetRequired ?? false;
        }
    }
}