using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Microsoft.AspNetCore.Routing;

using System.Buffers.Text;
using System.Security.Claims;
using Antiforgery;
using Http;
using Identity;
using Mvc;
using OrdoWiki.Data.Auth;
using OrdoWiki.Data.Entities;
using OrdoWiki.Web.Components.Account;
using OrdoWiki.Web.Components.Account.Shared;
using OrdoWiki.Web.Services.Contract;
using SignInResult = Identity.SignInResult;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        accountGroup.MapPost("/PasswordLogin", async (
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] ILoggerFactory loggerFactory,
            [FromForm] string? username,
            [FromForm] string? password,
            [FromForm] bool? rememberMe,
            [FromForm] string? returnUrl,
            [FromForm(Name = "Passkey.CredentialJson")] string? passkeyCredentialJson,
            [FromForm(Name = "Passkey.Error")] string? passkeyError) =>
        {
            ILogger logger = loggerFactory.CreateLogger("Login");

            if (!string.IsNullOrEmpty(passkeyError))
                return Results.LocalRedirect(BuildLoginRedirect("invalid", returnUrl));

            if (!string.IsNullOrEmpty(passkeyCredentialJson))
            {
                SignInResult passkeyResult = await signInManager.PasskeySignInAsync(passkeyCredentialJson);

                if (passkeyResult.Succeeded)
                {
                    logger.LogInformation("Passkey sign-in succeeded.");
                    return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
                }

                if (passkeyResult.IsLockedOut)
                    return Results.LocalRedirect("/Account/Lockout");

                logger.LogWarning("Passkey sign-in failed.");
                return Results.LocalRedirect(BuildLoginRedirect("invalid", returnUrl));
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return Results.LocalRedirect(BuildLoginRedirect("missing", returnUrl));

            SignInResult result = await signInManager.PasswordSignInAsync(
                username, password, rememberMe ?? false, false);

            if (result.Succeeded)
            {
                logger.LogInformation("User {Username} signed in.", username);
                return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User {Username} locked out.", username);
                return Results.LocalRedirect("/Account/Lockout");
            }

            return Results.LocalRedirect(BuildLoginRedirect("invalid", returnUrl));
        });

        accountGroup.MapPost("/PasswordRegister", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] IUserStore<ApplicationUser> userStore,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] InviteCodeService inviteCodes,
            [FromServices] ILoggerFactory loggerFactory,
            [FromForm] string code,
            [FromForm] string username,
            [FromForm] string? displayName,
            [FromForm] string password,
            [FromForm] string confirmPassword) =>
        {
            ILogger logger = loggerFactory.CreateLogger("PasswordRegister");

            string registerBack = string.IsNullOrEmpty(code)
                ? "/Account/Register"
                : $"/Account/Register?code={Uri.EscapeDataString(code)}";

            if (string.IsNullOrEmpty(code))
            {
                SetStatus(context, "Error: Missing invitation code.");
                return Results.LocalRedirect(registerBack);
            }

            if (password != confirmPassword)
            {
                SetStatus(context, "Error: Passwords don't match.");
                return Results.LocalRedirect(registerBack);
            }

            InviteCode? invite = await inviteCodes.FindUsableAsync(code);
            if (invite is null)
            {
                SetStatus(context, "Error: That invitation has expired or already been used.");
                return Results.LocalRedirect("/Account/Register");
            }

            ApplicationUser user = new()
            {
                UserName = username,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName
            };
            await userStore.SetUserNameAsync(user, username, CancellationToken.None);

            IdentityResult createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                SetStatus(context, "Error: " + string.Join(" ", createResult.Errors.Select(e => e.Description)));
                return Results.LocalRedirect(registerBack);
            }

            InviteCode? redeemed = await inviteCodes.RedeemAsync(code, await userManager.GetUserIdAsync(user));
            if (redeemed is null)
            {
                await userManager.DeleteAsync(user);
                SetStatus(context, "Error: That invitation expired or was used by someone else just now.");
                return Results.LocalRedirect("/Account/Register");
            }

            IdentityResult roleResult = await userManager.AddToRoleAsync(user, redeemed.AssignedRole);
            if (!roleResult.Succeeded)
            {
                SetStatus(context, "Error: " + string.Join(" ", roleResult.Errors.Select(e => e.Description)));
                return Results.LocalRedirect(registerBack);
            }

            logger.LogInformation("User {Username} registered as {Role} via invite.", username, redeemed.AssignedRole);
            await signInManager.SignInAsync(user, false);
            return Results.LocalRedirect("/");
        });

        accountGroup.MapPost("/PasskeyCreationOptions", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);

            ApplicationUser? user = await userManager.GetUserAsync(context.User);
            if (user is null)
                return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");

            string userId = await userManager.GetUserIdAsync(user);
            string userName = await userManager.GetUserNameAsync(user) ?? "User";
            string optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
            {
                Id = userId,
                Name = userName,
                DisplayName = user.DisplayName ?? userName
            });
            return TypedResults.Content(optionsJson, "application/json");
        });

        accountGroup.MapPost("/PasskeyRequestOptions", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IAntiforgery antiforgery,
            [FromQuery] string? username) =>
        {
            await antiforgery.ValidateRequestAsync(context);

            ApplicationUser? user = string.IsNullOrEmpty(username) ? null : await userManager.FindByNameAsync(username);
            string optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
            return TypedResults.Content(optionsJson, "application/json");
        });

        RouteGroupBuilder manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

        manageGroup.MapPost("/PasswordChange", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] ILoggerFactory loggerFactory,
            [FromForm] string oldPassword,
            [FromForm] string newPassword,
            [FromForm] string confirmPassword) =>
        {
            ApplicationUser? user = await userManager.GetUserAsync(context.User);
            if (user is null) return Results.LocalRedirect("/Account/Login");

            if (newPassword != confirmPassword)
            {
                SetStatus(context, "Error: Passwords don't match.");
                return Results.LocalRedirect("/Account/Manage/ChangePassword");
            }

            IdentityResult result = await userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                SetStatus(context, "Error: " + string.Join(" ", result.Errors.Select(e => e.Description)));
                return Results.LocalRedirect("/Account/Manage/ChangePassword");
            }

            bool wasForcedReset = user.IsPasswordResetRequired;
            if (wasForcedReset)
            {
                user.IsPasswordResetRequired = false;
                await userManager.UpdateAsync(user);
            }

            await signInManager.RefreshSignInAsync(user);
            ILogger logger = loggerFactory.CreateLogger("PasswordChange");
            logger.LogInformation("User {Username} changed their password.", user.UserName);

            if (wasForcedReset)
            {
                SetPasswordChangedSnackbar(context, "Your password has been changed.");
                return Results.LocalRedirect("/");
            }

            SetStatus(context, "Your password has been changed.");
            return Results.LocalRedirect("/Account/Manage/ChangePassword");
        });

        manageGroup.MapPost("/ProfileUpdate", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string? displayName) =>
        {
            ApplicationUser? user = await userManager.GetUserAsync(context.User);
            if (user is null) return Results.LocalRedirect("/Account/Login");

            string? newDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            if (newDisplayName != user.DisplayName)
            {
                user.DisplayName = newDisplayName;
                IdentityResult update = await userManager.UpdateAsync(user);
                if (!update.Succeeded)
                {
                    SetStatus(context, "Error: " + string.Join(" ", update.Errors.Select(e => e.Description)));
                    return Results.LocalRedirect("/Account/Manage");
                }
            }

            await signInManager.RefreshSignInAsync(user);
            SetStatus(context, "Your profile has been updated.");
            return Results.LocalRedirect("/Account/Manage");
        });

        manageGroup.MapPost("/Avatar", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] IMediaService media,
            IFormFile? file) =>
        {
            ApplicationUser? user = await userManager.GetUserAsync(context.User);
            if (user is null) return Results.LocalRedirect("/Account/Login");

            if (file is null || file.Length == 0)
            {
                SetStatus(context, "Error: No file selected.");
                return Results.LocalRedirect("/Account/Manage");
            }

            await using Stream stream = file.OpenReadStream();
            OrdoWiki.Web.Models.ApiResponse<string> result = await media.SaveAvatarAsync(
                stream, file.ContentType, file.Length);

            if (!result)
            {
                SetStatus(context, $"Error: {result.Error}");
                return Results.LocalRedirect("/Account/Manage");
            }

            user.AvatarPath = result.Value;
            IdentityResult update = await userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                SetStatus(context, "Error: " + string.Join(" ", update.Errors.Select(e => e.Description)));
                return Results.LocalRedirect("/Account/Manage");
            }

            await signInManager.RefreshSignInAsync(user);
            SetStatus(context, "Avatar updated.");
            return Results.LocalRedirect("/Account/Manage");
        });

        manageGroup.MapPost("/AvatarRemove", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromServices] SignInManager<ApplicationUser> signInManager) =>
        {
            ApplicationUser? user = await userManager.GetUserAsync(context.User);
            if (user is null) return Results.LocalRedirect("/Account/Login");

            if (user.AvatarPath is null)
                return Results.LocalRedirect("/Account/Manage");

            user.AvatarPath = null;
            IdentityResult update = await userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                SetStatus(context, "Error: " + string.Join(" ", update.Errors.Select(e => e.Description)));
                return Results.LocalRedirect("/Account/Manage");
            }

            await signInManager.RefreshSignInAsync(user);
            SetStatus(context, "Avatar removed.");
            return Results.LocalRedirect("/Account/Manage");
        });

        manageGroup.MapPost("/PasskeyRename", async (
            HttpContext context,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromForm] string id,
            [FromForm] string name) =>
        {
            ApplicationUser? user = await userManager.GetUserAsync(context.User);
            if (user is null) return Results.LocalRedirect("/Account/Login");

            byte[] credentialId;
            try
            {
                credentialId = Base64Url.DecodeFromChars(id);
            }
            catch (FormatException)
            {
                SetStatus(context, "Error: Invalid passkey ID.");
                return Results.LocalRedirect("/Account/Manage/Passkeys");
            }

            UserPasskeyInfo? passkey = await userManager.GetPasskeyAsync(user, credentialId);
            if (passkey is null)
            {
                SetStatus(context, "Error: Passkey not found.");
                return Results.LocalRedirect("/Account/Manage/Passkeys");
            }

            passkey.Name = name;
            IdentityResult result = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
            if (!result.Succeeded)
            {
                SetStatus(context, "Error: Could not rename passkey.");
                return Results.LocalRedirect("/Account/Manage/Passkeys");
            }

            SetStatus(context, "Passkey renamed.");
            return Results.LocalRedirect("/Account/Manage/Passkeys");
        });

        return accountGroup;
    }

    private static string BuildLoginRedirect(string error, string? returnUrl)
    {
        string url = $"/Account/Login?error={Uri.EscapeDataString(error)}";
        if (!string.IsNullOrEmpty(returnUrl)) url += $"&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        return url;
    }

    private static void SetStatus(HttpContext context, string message)
    {
        context.Response.Cookies.Append(IdentityRedirectManager.StatusCookieName, message, new CookieOptions
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromSeconds(5)
        });
    }

    private static void SetPasswordChangedSnackbar(HttpContext context, string message)
    {
        context.Response.Cookies.Append(StatusSnackbar.CookieName, message, new CookieOptions
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromSeconds(5)
        });
    }
}