using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrdoWiki.Data.Auth;

namespace OrdoWiki.Data;

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
