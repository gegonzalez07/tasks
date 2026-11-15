using Serilog;
using Tasks.Api.Configuration;
using Tasks.Application;
using Tasks.Infrastructure;
using Tasks.Infrastructure.Persistence.Seeding;

// Minimal console logger for startup failures; replaced by the configured Serilog pipeline below.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration, builder.Environment)
        .AddApiPresentation();

    var app = builder.Build();

    // Request logging wraps the exception handler so it records the final status code (e.g. 401, not 500).
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages(); // ProblemDetails bodies for empty 401/403/404 responses

    app.UseIdentityServer(); // also runs UseAuthentication()
    app.UseAuthorization();

    app.UseApiEndpoints();

    await app.Services.InitializeDatabaseAsync();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Tasks API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposed for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.</summary>
public partial class Program
{
}
