using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tasks.Application.TaskItems;
using Tasks.IntegrationTests.Infrastructure;

namespace Tasks.IntegrationTests.Endpoints.V1;

/// <summary>
/// "Today" for the due-date rules is the caller's date (X-Time-Zone header), not the server's UTC date.
/// The tests pick a zone whose date differs from UTC at the factory's frozen instant, so they are
/// meaningful whatever the time of day they run.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RequestTimeZoneTests(TasksApiFactory factory)
{
    private const string TasksRoute = "/api/v1/tasks";
    private const string Header = "X-Time-Zone";

    [Fact]
    public async Task Create_DueDateIsCheckedAgainstTheCallersToday()
    {
        var (zoneId, zone) = ZoneWithDifferentDate();
        var localToday = factory.TodayIn(zone);
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();
        client.DefaultRequestHeaders.Add(Header, zoneId);

        using var today = await PostAsync(client, localToday);
        using var yesterday = await PostAsync(client, localToday.AddDays(-1));

        Assert.Equal(HttpStatusCode.Created, today.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, yesterday.StatusCode);
        await AssertHasErrorAsync(yesterday, "dueDate");
    }

    [Fact]
    public async Task Create_WithoutHeader_UsesUtc()
    {
        var (_, zone) = ZoneWithDifferentDate();
        var localToday = factory.TodayIn(zone);
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();

        using var response = await PostAsync(client, localToday);

        // The caller's "today" is only valid in UTC when it is not before the UTC date.
        var expected = localToday >= factory.UtcToday ? HttpStatusCode.Created : HttpStatusCode.BadRequest;
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Update_MovingDueDateToTheCallersToday_IsAllowed()
    {
        var (zoneId, zone) = ZoneWithDifferentDate();
        var localToday = factory.TodayIn(zone);
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();
        client.DefaultRequestHeaders.Add(Header, zoneId);
        using var created = await PostAsync(client, localToday.AddDays(10));
        var task = (await created.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options))!;

        using var toToday = await client.PutAsJsonAsync($"{TasksRoute}/{task.Id}", Body(localToday, "InProgress"));
        using var toYesterday = await client.PutAsJsonAsync($"{TasksRoute}/{task.Id}", Body(localToday.AddDays(-1), "InProgress"));

        Assert.Equal(HttpStatusCode.OK, toToday.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, toYesterday.StatusCode);
    }

    [Theory]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("not a zone")]
    public async Task UnknownTimeZone_Returns400(string zoneId)
    {
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();
        client.DefaultRequestHeaders.Add(Header, zoneId);

        using var response = await PostAsync(client, factory.UtcToday.AddDays(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertHasErrorAsync(response, Header);
    }

    [Fact]
    public async Task UnknownTimeZone_WithoutToken_IsStill401()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(Header, "Mars/Olympus_Mons");

        using var response = await client.GetAsync(TasksRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// UTC-12 is a day behind UTC before 12:00 UTC; UTC+14 is a day ahead from 10:00 UTC.
    /// One of them always gives a date different from UTC.
    /// </summary>
    private (string Id, TimeZoneInfo Zone) ZoneWithDifferentDate()
    {
        var zoneId = factory.Now.UtcDateTime.Hour < 12 ? "Etc/GMT+12" : "Pacific/Kiritimati";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        Assert.NotEqual(factory.UtcToday, factory.TodayIn(zone));
        return (zoneId, zone);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, DateOnly dueDate) =>
        client.PostAsJsonAsync(TasksRoute, Body(dueDate, status: null));

    private static object Body(DateOnly dueDate, string? status) => new
    {
        title = "Time zone task",
        assignee = $"user-{Guid.NewGuid():N}",
        status,
        dueDate = dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    };

    private static async Task AssertHasErrorAsync(HttpResponseMessage response, string field)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }
}
