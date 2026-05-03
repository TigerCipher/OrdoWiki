namespace OrdoWiki.Data;

using Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class DataContextRegistration
{
    public static IServiceCollection AddOrdoWikiData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        services.AddScoped<InviteCodeService>();
        services.AddHostedService<IdentityBootstrapper>();

        return services;
    }
}