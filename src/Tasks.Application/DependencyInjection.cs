using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tasks.Application.Auth;
using Tasks.Application.Common.Time;
using Tasks.Application.TaskItems;

namespace Tasks.Application;

public static class DependencyInjection
{
    /// <summary>Registers application-layer services (use cases and validators).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IRequestTimeZone, UtcRequestTimeZone>(); // the API replaces it with the request's zone
        services.TryAddSingleton<IClientCalendar, ClientCalendar>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Singleton);

        return services;
    }
}
