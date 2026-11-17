using Tasks.Application.Common.Time;

namespace Tasks.Api.TimeZones;

/// <summary>
/// <see cref="IRequestTimeZone"/> for HTTP requests: the zone resolved by <see cref="RequestTimeZoneMiddleware"/>,
/// or UTC when the request did not send <c>X-Time-Zone</c> (or there is no request).
/// </summary>
internal sealed class HttpRequestTimeZone(IHttpContextAccessor httpContextAccessor) : IRequestTimeZone
{
    public TimeZoneInfo TimeZone =>
        httpContextAccessor.HttpContext?.Items.TryGetValue(RequestTimeZoneMiddleware.ItemKey, out var value) == true
        && value is TimeZoneInfo zone
            ? zone
            : TimeZoneInfo.Utc;
}
