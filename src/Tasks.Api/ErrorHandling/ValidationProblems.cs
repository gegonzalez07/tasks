using System.Diagnostics;
using System.Text.Json;
using FluentValidation.Results;

namespace Tasks.Api.ErrorHandling;

/// <summary>
/// The single shape of every 400 validation response (RFC 9457 + ASP.NET Core's <see cref="HttpValidationProblemDetails"/>):
/// <c>{ type, title, status, instance, traceId, errors: { "field": ["message", ...] } }</c>.
/// Used by FastEndpoints (request validation and binding errors), by <see cref="GlobalExceptionHandler"/>
/// (validation raised by services) and by middleware, so clients only handle one format.
/// </summary>
internal static class ValidationProblems
{
    public const string Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
    public const string Title = "One or more validation errors occurred.";

    public static HttpValidationProblemDetails Create(
        IEnumerable<ValidationFailure> failures,
        HttpContext context,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        ArgumentNullException.ThrowIfNull(failures);

        var errors = failures
            .GroupBy(f => ToFieldName(f.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return Create(errors, context, statusCode);
    }

    public static HttpValidationProblemDetails Create(
        IDictionary<string, string[]> errors,
        HttpContext context,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(context);

        var problem = new HttpValidationProblemDetails(errors)
        {
            Type = Type,
            Title = Title,
            Status = statusCode,
            Instance = $"{context.Request.Method} {context.Request.Path}",
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        return problem;
    }

    /// <summary>Field names as they appear in the JSON body (camelCase): "DueDate" → "dueDate".</summary>
    private static string ToFieldName(string? propertyName) =>
        string.IsNullOrWhiteSpace(propertyName) ? "request" : JsonNamingPolicy.CamelCase.ConvertName(propertyName);
}
