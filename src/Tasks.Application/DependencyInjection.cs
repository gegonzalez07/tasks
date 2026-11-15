using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tasks.Application.Auth;

namespace Tasks.Application;

public static class DependencyInjection
{
    /// <summary>Registers application-layer services (use cases and validators).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAuthService, AuthService>();
        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Singleton);

        return services;
    }
}
