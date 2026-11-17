using Tasks.Api.ErrorHandling;

namespace Tasks.Api.TimeZones;

/// <summary>
/// Reads the optional <c>X-Time-Zone</c> header (IANA id, e.g. <c>America/Bogota</c>) so that date rules use the
/// caller's "today". Without the header the request is evaluated in UTC. An unknown zone is a 400 in the
/// standard validation format.
/// </summary>
internal sealed class RequestTimeZoneMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Time-Zone";
    private const int MaxLength = 64;

    internal static readonly object ItemKey = new();

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetailsService)
    {
        var value = context.Request.Headers[HeaderName].ToString().Trim();

        if (value.Length > 0)
        {
            if (value.Length > MaxLength || !TimeZoneInfo.TryFindSystemTimeZoneById(value, out var zone))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var problem = ValidationProblems.Create(
                    new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        [HeaderName] = ["Unknown time zone. Use an IANA id such as 'America/Bogota' or 'Europe/Madrid'."],
                    },
                    context);

                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails = problem,
                });
                return;
            }

            context.Items[ItemKey] = zone;
        }

        await next(context);
    }
}
