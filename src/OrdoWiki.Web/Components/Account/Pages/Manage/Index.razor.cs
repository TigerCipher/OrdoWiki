using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using OrdoWiki.Data.Entities;

namespace OrdoWiki.Web.Components.Account.Pages.Manage;

public partial class Index
{
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    private string Username { get; set; } = "";
    private string DisplayName { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        if (HttpContext is null)
        {
            return;
        }

        ApplicationUser? user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            return;
        }

        Username = user.UserName ?? "";
        DisplayName = user.DisplayName ?? user.UserName ?? "";
    }
}
