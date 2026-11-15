using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tasks.Application.Auth.Abstractions;
using Tasks.Application.Users;
using Tasks.Domain.Repositories;
using Tasks.Infrastructure.Identity;
using Tasks.Infrastructure.Options;
using Tasks.Infrastructure.Persistence;
using Tasks.Infrastructure.Persistence.Repositories;
using Tasks.Infrastructure.Persistence.Seeding;

namespace Tasks.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.TryAddSingleton(TimeProvider.System);

        services.Configure<AuthenticationSettings>(configuration.GetSection(AuthenticationSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<AdminSeedSettings>(configuration.GetSection(AdminSeedSettings.SectionName));

        services
            .AddPersistence(configuration)
            .AddIdentityAndTokenServer(configuration, environment);

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        var databaseSettings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>() ?? new DatabaseSettings();

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(databaseSettings.MaxRetryCount);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<DbInitializer>();

        return services;
    }

    private static IServiceCollection AddIdentityAndTokenServer(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var authentication = configuration.GetSection(AuthenticationSettings.SectionName).Get<AuthenticationSettings>()
            ?? new AuthenticationSettings();

        // ASP.NET Core Identity (user store)
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Duende IdentityServer (token authority) embedded in the API host
        var identityServer = services
            .AddIdentityServer(options =>
            {
                options.IssuerUri = authentication.Issuer;
                options.KeyManagement.Enabled = false; // explicit signing credential below
                options.EmitStaticAudienceClaim = false;
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseFailureEvents = true;
            })
            .AddInMemoryIdentityResources(IdentityServerConfig.GetIdentityResources())
            .AddInMemoryApiScopes(IdentityServerConfig.GetApiScopes(authentication))
            .AddInMemoryApiResources(IdentityServerConfig.GetApiResources(authentication))
            .AddInMemoryClients(IdentityServerConfig.GetClients(authentication))
            .AddAspNetIdentity<ApplicationUser>();

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            // Development key: persisted to <content root>/keys in Development, ephemeral in tests.
            // Without a signing credential IdentityServer fails with "No signing credential registered".
            var keyFile = Path.Combine(environment.ContentRootPath, "keys", "tempkey.jwk");
            var persistKey = environment.IsDevelopment();
            if (persistKey)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(keyFile)!);
            }

            identityServer.AddDeveloperSigningCredential(persistKey, keyFile);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(authentication.SigningCertificatePath))
            {
                throw new InvalidOperationException(
                    "Authentication:SigningCertificatePath must point to a PFX certificate outside Development/Testing.");
            }

            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                authentication.SigningCertificatePath,
                authentication.SigningCertificatePassword);
            identityServer.AddSigningCredential(certificate);
        }

        // The API itself is a resource server: bearer tokens are the default scheme
        // (AddIdentity sets cookie schemes as defaults, so they are overridden explicitly).
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenIssuer, IdentityServerTokenIssuer>();
        services.AddScoped<IUserQueryService, UserQueryService>();

        return services;
    }
}
