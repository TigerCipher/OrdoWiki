using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using OrdoWiki.Data;
using OrdoWiki.Data.Auth;
using OrdoWiki.Data.Entities;
using OrdoWiki.Web;
using OrdoWiki.Web.Components;
using OrdoWiki.Web.Components.Account;

// `dotnet OrdoWiki.Web.dll --migrate` — apply pending EF migrations and exit.
// Used by the migrator service in deploy/docker-compose.yml so the app container
// always boots against an up-to-date schema without racing other replicas.
if (args.Contains("--migrate"))
{
    WebApplicationBuilder migrateBuilder = WebApplication.CreateBuilder(args);
    string migrateConnection = migrateBuilder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    migrateBuilder.Services.AddOrdoWikiData(migrateConnection);

    using WebApplication migrateApp = migrateBuilder.Build();
    using IServiceScope scope = migrateApp.Services.CreateScope();
    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Applying pending migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("Migrations complete.");
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();

// Caddy terminates TLS and forwards plain HTTP to us. Without this, Request.Scheme
// stays "http", UseHttpsRedirection ping-pongs, and Identity post-login redirects
// build wrong absolute URLs. KnownNetworks/Proxies cleared because the docker
// network IP isn't loopback.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Persist Data Protection keys to a mounted volume so Identity auth cookies and
// antiforgery tokens survive container restarts. Default location is inside the
// container's filesystem and gets wiped on every redeploy.
string dpKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "dpkeys");
Directory.CreateDirectory(dpKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetApplicationName("OrdoWiki");

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

// Must be the very first middleware so HttpsRedirection / HSTS / Auth see the
// real client scheme + IP from Caddy.
app.UseForwardedHeaders();

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
