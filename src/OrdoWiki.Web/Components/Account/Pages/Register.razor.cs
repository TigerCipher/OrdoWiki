using Microsoft.AspNetCore.Components;
using OrdoWiki.Data.Auth;
using OrdoWiki.Data.Entities;

namespace OrdoWiki.Web.Components.Account.Pages;

public partial class Register
{
    [Inject] private InviteCodeService InviteCodes { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "code")] private string? CodeFromQuery { get; set; }

    private InviteCode? Invite { get; set; }

    private string Username { get; set; } = "";
    private string DisplayName { get; set; } = "";
    private string Password { get; set; } = "";
    private string ConfirmPassword { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrWhiteSpace(CodeFromQuery))
        {
            Invite = await InviteCodes.FindUsableAsync(CodeFromQuery);
        }
    }
}
