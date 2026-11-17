using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Tasks.Api.ErrorHandling;
using Tasks.Api.TimeZones;

namespace Tasks.Api.Configuration;

public static class ApiApplicationBuilderExtensions
{
    public const string RoutePrefix = "api";

    /// <summary>
    /// Maps every FastEndpoint under <c>/api/v{version}/...</c> with RFC 9457 validation errors
    /// in the single format produced by <see cref="ValidationProblems"/>.
    /// </summary>
    public static WebApplication UseApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // After authentication/authorization (so a missing token is still a 401) and before the endpoints.
        app.UseMiddleware<RequestTimeZoneMiddleware>();

        app.UseFastEndpoints(config =>
        {
            config.Endpoints.RoutePrefix = RoutePrefix;
            config.Endpoints.ShortNames = true;
            config.Versioning.Prefix = "v";
            config.Versioning.PrependToRoute = true;
            config.Versioning.DefaultVersion = ApiVersions.V1;
            // Same 400 shape as the GlobalExceptionHandler (see ValidationProblems).
            config.Errors.ResponseBuilder = (failures, httpContext, statusCode) =>
                ValidationProblems.Create(failures, httpContext, statusCode);

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
