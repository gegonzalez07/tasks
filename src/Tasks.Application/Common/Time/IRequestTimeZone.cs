namespace Tasks.Application.Common.Time;

/// <summary>
/// Time zone of the caller of the current operation. Used to decide what "today" means for date rules
/// (e.g. a due date cannot be in the past). Without information about the caller it is UTC.
/// </summary>
public interface IRequestTimeZone
{
    TimeZoneInfo TimeZone { get; }
}
