using Microsoft.Extensions.DependencyInjection;

namespace Tasks.Infrastructure.Persistence.Seeding;

public static class DatabaseInitializerExtensions
{
    /// <summary>Applies the schema and seeds the default data inside a dedicated DI scope.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
