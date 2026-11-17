using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tasks.Application.Common.Time;
using Tasks.Infrastructure.Persistence;

namespace Tasks.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API (Program.cs) in memory: same pipeline, IdentityServer, FastEndpoints and seeding,
/// with SQL Server swapped for a shared SQLite in-memory database.
/// "Today" for the date rules is frozen at <see cref="Now"/> so tests never break when the date changes mid-run
/// (tokens, auditing and everything else keep using the real clock).
/// </summary>
public sealed class TasksApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUserName = "admin";
    public const string AdminEmail = "admin@tests.local";
    public const string AdminPassword = "Adm1n!Integration#2026";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>The instant the API uses to decide what "today" is (real time when the factory was created).</summary>
    public DateTimeOffset Now { get; } = TimeProvider.System.GetUtcNow();

    public DateOnly UtcToday => TodayIn(TimeZoneInfo.Utc);

    /// <summary>"Today" as the API sees it for a request sent with that time zone.</summary>
    public DateOnly TodayIn(TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, zone).DateTime);

    public TasksApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseEnvironment / UseSetting are visible to Program.cs before builder.Build().
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:InitializationStrategy", "EnsureCreated");
        builder.UseSetting("Seed:Admin:UserName", AdminUserName);
        builder.UseSetting("Seed:Admin:Email", AdminEmail);
        builder.UseSetting("Seed:Admin:Password", AdminPassword);
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");

        builder.ConfigureTestServices(services =>
        {
            // Replace the SQL Server registration (EF Core 9+ also registers IDbContextOptionsConfiguration<T>).
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // Same calendar implementation as production, with a frozen clock and the real request time zone.
            var frozenClock = new FrozenTimeProvider(Now);
            services.RemoveAll<IClientCalendar>();
            services.AddSingleton<IClientCalendar>(provider =>
                new ClientCalendar(frozenClock, provider.GetRequiredService<IRequestTimeZone>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TasksApiFactory>
{
    public const string Name = "Tasks API";
}
