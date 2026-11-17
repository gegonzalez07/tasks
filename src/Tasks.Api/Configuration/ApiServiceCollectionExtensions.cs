using System.Diagnostics;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Swagger;
using Tasks.Api.ErrorHandling;

namespace Tasks.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    /// <summary>Presentation-layer services: FastEndpoints, OpenAPI documents and RFC 9457 error handling.</summary>
    public static IServiceCollection AddApiPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();

        // Endpoints live in this assembly; validators live in Application and inherit
        // FluentValidation.AbstractValidator<T>, which FastEndpoints only registers when
        // IncludeAbstractValidators is enabled (by default it only discovers Validator<T>).
        services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(Program).Assembly, typeof(Application.DependencyInjection).Assembly];
            options.IncludeAbstractValidators = true;
        });

        foreach (var version in ApiVersions.Published)
        {
            services.SwaggerDocument(options =>
            {
                options.MaxEndpointVersion = version;
                options.ShortSchemaNames = true;
                options.EnableJWTBearerAuth = true;
                options.AutoTagPathSegmentIndex = 0; // tags are declared explicitly per endpoint
                options.SerializerSettings = serializer => serializer.Converters.Add(new JsonStringEnumConverter());
                options.DocumentSettings = settings =>
                {
                    settings.DocumentName = $"v{version}";
                    settings.Title = "Tasks API";
                    settings.Version = $"v{version}";
                    settings.Description =
                        "Tasks Web API. Obtain a token at POST /api/v1/auth/token and send it as 'Authorization: Bearer {token}'.";
                };
            });
        }

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var request = context.HttpContext.Request;
                context.ProblemDetails.Instance ??= $"{request.Method} {request.Path}";
                context.ProblemDetails.Extensions.TryAdd(
                    "traceId",
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
