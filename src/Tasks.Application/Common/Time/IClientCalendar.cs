namespace Tasks.Application.Common.Time;

/// <summary>Calendar date as seen by the caller (current instant converted to the caller's time zone).</summary>
public interface IClientCalendar
{
    DateOnly Today { get; }

    TimeZoneInfo TimeZone { get; }
}
