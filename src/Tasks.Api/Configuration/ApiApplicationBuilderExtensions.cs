using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace Tasks.Api.Configuration;

public static class ApiApplicationBuilderExtensions
{
    public const string RoutePrefix = "api";

    /// <summary>
    /// Maps every FastEndpoint under <c>/api/v{version}/...</c> with RFC 9457 validation errors.
    /// </summary>
    public static WebApplication UseApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = RoutePrefix;
            config.Endpoints.ShortNames = true;
            config.Versioning.Prefix = "v";
            config.Versioning.PrependToRoute = true;
            config.Versioning.DefaultVersion = ApiVersions.V1;
            config.Errors.UseProblemDetails();

            // Enums travel as text ("InProgress") in requests and responses.
            config.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
        });

        if (!app.Environment.IsProduction())
        {
            app.UseSwaggerGen();
        }

        return app;
    }
}
