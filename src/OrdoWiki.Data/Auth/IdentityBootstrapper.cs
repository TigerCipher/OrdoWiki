namespace OrdoWiki.Data.Auth;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class IdentityBootstrapper(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<IdentityBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

#if DEBUG
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IEnumerable<string> pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            logger.LogInformation("Applying pending migrations: {Migrations}", string.Join(", ", pending));
            await db.Database.MigrateAsync(cancellationToken);
        }
#endif

        RoleManager<IdentityRole> roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (string role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                IdentityResult result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                    logger.LogInformation("Seeded role {Role}.", role);
                else
                {
                    logger.LogError("Failed to seed role {Role}: {Errors}", role,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (userManager.Users.Any()) return;

        string? username = configuration["Admin:Username"];
        string? password = configuration["Admin:InitialPassword"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No users exist and Admin:Username / Admin:InitialPassword are not configured. " +
                "Set both in configuration to bootstrap the first admin.");
            return;
        }

        ApplicationUser admin = new()
        {
            UserName = username,
            DisplayName = username,
            IsPasswordResetRequired = true
        };

        IdentityResult createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create bootstrap admin {Username}: {Errors}",
                username,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        IdentityResult roleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign Admin role to bootstrap admin {Username}: {Errors}",
                username,
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Bootstrap admin {Username} created. Password reset is required on first login.",
            username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}