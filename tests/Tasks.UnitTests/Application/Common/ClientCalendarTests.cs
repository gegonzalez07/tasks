using Moq;
using Tasks.Application.Common.Time;

namespace Tasks.UnitTests.Application.Common;

public sealed class ClientCalendarTests
{
    // 01:30 UTC on 18 Nov: still 17 Nov in America (UTC-5), already 18 Nov in UTC and Europe.
    private static readonly DateTimeOffset Instant = new(2026, 11, 18, 1, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-5, 2026, 11, 17)]
    [InlineData(0, 2026, 11, 18)]
    [InlineData(1, 2026, 11, 18)]
    [InlineData(-12, 2026, 11, 17)]
    [InlineData(14, 2026, 11, 18)]
    public void Today_IsTheDateInTheRequestTimeZone(int offsetHours, int year, int month, int day)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone($"Test{offsetHours}", TimeSpan.FromHours(offsetHours), "Test", "Test");

        var calendar = CreateCalendar(Instant, zone);

        Assert.Equal(new DateOnly(year, month, day), calendar.Today);
        Assert.Same(zone, calendar.TimeZone);
    }

    [Fact]
    public void Today_FollowsTheClock()
    {
        var clock = new Mock<TimeProvider>();
        var requestTimeZone = new Mock<IRequestTimeZone>();
        requestTimeZone.Setup(z => z.TimeZone).Returns(TimeZoneInfo.Utc);
        var calendar = new ClientCalendar(clock.Object, requestTimeZone.Object);

        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 17, 23, 59, 59, TimeSpan.Zero));
        var before = calendar.Today;
        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero));
        var after = calendar.Today;

        Assert.Equal(new DateOnly(2026, 11, 17), before);
        Assert.Equal(new DateOnly(2026, 11, 18), after);
    }

    private static ClientCalendar CreateCalendar(DateTimeOffset now, TimeZoneInfo zone)
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(c => c.GetUtcNow()).Returns(now);
        var requestTimeZone = new Mock<IRequestTimeZone>();
        requestTimeZone.Setup(z => z.TimeZone).Returns(zone);
        return new ClientCalendar(clock.Object, requestTimeZone.Object);
    }
}
