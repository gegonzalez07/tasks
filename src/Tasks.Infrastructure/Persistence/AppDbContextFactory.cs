using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tasks.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time (migrations). Override the connection string with the
/// <c>TASKS_CONNECTION_STRING</c> environment variable.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnectionString =
        "Server=localhost,1433;Database=TasksDb;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TASKS_CONNECTION_STRING") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options, TimeProvider.System);
    }
}
