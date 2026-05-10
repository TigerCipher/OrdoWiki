using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using OrdoWiki.Data;
using OrdoWiki.Data.Auth;
using OrdoWiki.Data.Entities;
using OrdoWiki.Web;
using OrdoWiki.Web.Components;
using OrdoWiki.Web.Components.Account;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.CanEdit, p => p.RequireRole(Roles.Admin, Roles.Designer, Roles.Editor))
    .AddPolicy(Policies.CanDesign, p => p.RequireRole(Roles.Admin, Roles.Designer));

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services
    .AddOrdoWikiData(connectionString)
    .AddDatabaseDeveloperPageExceptionFilter()
    .RegisterOrdoServices()
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<OrdoWikiUserClaimsPrincipalFactory>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseMigrationsEndPoint();
else
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseMiddleware<RequirePasswordChangeMiddleware>();

app.MapStaticAssets();

// Serve user-uploaded media. The folder is created on first upload, but make sure it
// exists at startup so the file provider doesn't throw on a missing directory.
string uploadsRoot = builder.Configuration["UploadsRoot"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(uploadsRoot);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Filenames are content-addressed (random IDs) so they never change.
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();