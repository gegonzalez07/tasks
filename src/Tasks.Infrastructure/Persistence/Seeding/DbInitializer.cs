using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tasks.Application.Common.Security;
using Tasks.Infrastructure.Identity;
using Tasks.Infrastructure.Options;

namespace Tasks.Infrastructure.Persistence.Seeding;

/// <summary>
/// Prepares the database at startup: applies the schema (migrations / EnsureCreated) and seeds
/// the <c>Admin</c> role plus the default administrator. Idempotent.
/// </summary>
public sealed class DbInitializer(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<DatabaseSettings> databaseSettings,
    IOptions<AdminSeedSettings> adminSettings,
    ILogger<DbInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await SeedRolesAsync().ConfigureAwait(false);
        await SeedAdministratorAsync().ConfigureAwait(false);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        switch (databaseSettings.Value.InitializationStrategy)
        {
            case DatabaseInitializationStrategy.Migrate when context.Database.GetMigrations().Any():
                logger.LogInformation("Applying pending EF Core migrations");
                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                break;

            case DatabaseInitializationStrategy.Migrate:
                logger.LogWarning(
                    "No EF Core migrations found; creating the schema with EnsureCreated. " +
                    "Run 'dotnet ef migrations add InitialCreate' before deploying.");
                await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                break;

            case DatabaseInitializationStrategy.EnsureCreated:
                await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                break;

            case DatabaseInitializationStrategy.None:
            default:
                break;
        }
    }

    private async Task SeedRolesAsync()
    {
        foreach (var roleName in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName).ConfigureAwait(false))
            {
                continue;
            }

            EnsureSucceeded(await roleManager.CreateAsync(new ApplicationRole(roleName)).ConfigureAwait(false), $"create role '{roleName}'");
            logger.LogInformation("Role {Role} created", roleName);
        }
    }

    private async Task SeedAdministratorAsync()
    {
        var settings = adminSettings.Value;

        var admin = await userManager.FindByEmailAsync(settings.Email).ConfigureAwait(false)
            ?? await userManager.FindByNameAsync(settings.UserName).ConfigureAwait(false);

        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                logger.LogWarning(
                    "Default administrator not created: configure 'Seed:Admin:Password' (user-secrets or environment variable)");
                return;
            }

            admin = new ApplicationUser(settings.UserName)
            {
                Email = settings.Email,
                EmailConfirmed = true,
                FullName = settings.FullName,
                IsActive = true,
            };

            EnsureSucceeded(await userManager.CreateAsync(admin, settings.Password).ConfigureAwait(false), "create the default administrator");
            logger.LogInformation("Default administrator {UserName} created", admin.UserName);
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin).ConfigureAwait(false))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, AppRoles.Admin).ConfigureAwait(false), "assign the Admin role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Could not {operation}: {errors}");
        }
    }
}
