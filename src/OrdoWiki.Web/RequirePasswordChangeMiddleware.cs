using OrdoWiki.Data.Auth;

namespace OrdoWiki.Web;

public class RequirePasswordChangeMiddleware(RequestDelegate next)
{
    private const string ChangePasswordPath = "/Account/Manage/ChangePassword";

    private static readonly string[] AllowedPaths =
    [
        ChangePasswordPath,
        "/Account/Manage/PasswordChange",   // POST endpoint behind ChangePassword form
        "/Account/Logout",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(c => c.Type == OrdoWikiClaims.MustChangePassword))
        {
            string path = context.Request.Path.Value ?? "";
            bool isAllowed =
                AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
                path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/images", StringComparison.OrdinalIgnoreCase);

            if (!isAllowed)
            {
                context.Response.Redirect(ChangePasswordPath);
                return;
            }
        }

        await next(context);
    }
}
