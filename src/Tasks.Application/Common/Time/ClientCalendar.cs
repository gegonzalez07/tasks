namespace Tasks.Application.Common.Time;

/// <summary>
/// "Today" for the caller: <see cref="TimeProvider"/> gives the instant (testable), <see cref="IRequestTimeZone"/>
/// the zone. Both are read on every access, so a singleton instance is safe.
/// </summary>
public sealed class ClientCalendar(TimeProvider clock, IRequestTimeZone requestTimeZone) : IClientCalendar
{
    public TimeZoneInfo TimeZone => requestTimeZone.TimeZone;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZone).DateTime);
}
