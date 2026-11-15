using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Tasks.Application.Common.Exceptions;
using Tasks.Domain.Exceptions;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Tasks.Api.ErrorHandling;

/// <summary>
/// Translates unhandled exceptions into RFC 9457 <c>application/problem+json</c> responses.
/// Unexpected errors (5xx) are logged as errors and their details are hidden outside Development.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (status, title, type) = Map(exception, httpContext);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} failed with {StatusCode}: {Message}", httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);
        }

        var problem = new MvcProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = status >= StatusCodes.Status500InternalServerError && !environment.IsDevelopment()
                ? "An unexpected error occurred. Use the traceId when contacting support."
                : exception.Message,
        };

        switch (exception)
        {
            case DomainException domainException:
                problem.Extensions["errorCode"] = domainException.ErrorCode;
                break;
            case ValidationException validationException:
                problem.Extensions["errors"] = validationException.Errors
                    .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray(), StringComparer.Ordinal);
                break;
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        }).ConfigureAwait(false);
    }

    private static (int Status, string Title, string Type) Map(Exception exception, HttpContext httpContext) => exception switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred.", "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
        BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request.", "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
        AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Authentication failed.", "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden.", "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found.", "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict.", "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
        DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violation.", "https://tools.ietf.org/html/rfc9110#section-15.5.21"),
        OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
            (499, "Client closed request.", "about:blank"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "https://tools.ietf.org/html/rfc9110#section-15.6.1"),
    };
}
