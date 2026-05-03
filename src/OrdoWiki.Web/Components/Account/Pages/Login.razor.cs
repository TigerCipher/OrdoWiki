using Microsoft.AspNetCore.Components;

namespace OrdoWiki.Web.Components.Account.Pages;

public partial class Login
{
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }
    [SupplyParameterFromQuery] private string? Error { get; set; }

    private string Username { get; set; } = "";
    private string Password { get; set; } = "";
    private bool RememberMe { get; set; }

    private string? ErrorMessage => Error switch
    {
        "invalid" => "Invalid sign-in attempt.",
        "missing" => "Username and password are required.",
        _ => null,
    };

    private void Test()
    {
        Console.WriteLine($"Username: {Username}");
    }
}
