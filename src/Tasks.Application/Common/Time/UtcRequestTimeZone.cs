namespace Tasks.Application.Common.Time;

/// <summary>Default when there is no request context (background jobs, tests): UTC.</summary>
internal sealed class UtcRequestTimeZone : IRequestTimeZone
{
    public TimeZoneInfo TimeZone => TimeZoneInfo.Utc;
}
