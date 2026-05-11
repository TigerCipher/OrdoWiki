namespace OrdoWiki.Data;

using Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DataContextRegistration
{
    public static IServiceCollection AddOrdoWikiData(this IServiceCollection services, string connectionString)
    {
        // Register the factory (singleton) — it owns the singleton DbContextOptions.
        // Then provide a scoped DbContext derived from the factory so existing scoped
        // consumers (Identity, the rest of our services) keep working unchanged, while
        // services that may render concurrently (BannerService, SiteThemeService) can
        // call CreateDbContextAsync() to get a short-lived per-call context.
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ApplicationDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

        services.AddScoped<InviteCodeService>();
        services.AddHostedService<IdentityBootstrapper>();

        return services;
    }
}
