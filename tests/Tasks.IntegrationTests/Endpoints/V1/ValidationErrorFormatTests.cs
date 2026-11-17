using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tasks.Application.TaskItems;
using Tasks.IntegrationTests.Infrastructure;

namespace Tasks.IntegrationTests.Endpoints.V1;

/// <summary>
/// Every 400 has the same shape, whatever produced it: FastEndpoints request validation, a service
/// (ValidationException through the GlobalExceptionHandler) or the X-Time-Zone middleware.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ValidationErrorFormatTests(TasksApiFactory factory)
{
    private const string TasksRoute = "/api/v1/tasks";
    private static readonly string[] ExpectedTopLevel = ["type", "title", "status", "instance", "traceId", "errors"];

    [Fact]
    public async Task BadRequests_FromValidatorServiceAndMiddleware_ShareTheSameShape()
    {
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();
        var created = await CreateTaskAsync(client, factory.UtcToday.AddDays(5));

        // 1. FastEndpoints request validation
        using var fromValidator = await client.PostAsJsonAsync(TasksRoute, new { title = "", assignee = "ana", dueDate = Date(factory.UtcToday) });

        // 2. TaskService (moving the due date into the past) -> ValidationException -> GlobalExceptionHandler
        using var fromService = await client.PutAsJsonAsync($"{TasksRoute}/{created.Id}", new
        {
            title = "Task",
            assignee = "ana",
            status = "Pending",
            dueDate = Date(factory.UtcToday.AddDays(-1)),
        });

        // 3. Query-string validation of the list
        using var fromQuery = await client.GetAsync($"{TasksRoute}?pageSize=0");

        // 4. RequestTimeZoneMiddleware
        using var request = new HttpRequestMessage(HttpMethod.Get, TasksRoute);
        request.Headers.Add("X-Time-Zone", "Mars/Olympus_Mons");
        using var fromMiddleware = await client.SendAsync(request);

        await AssertValidationProblemAsync(fromValidator, "title");
        await AssertValidationProblemAsync(fromService, "dueDate");
        await AssertValidationProblemAsync(fromQuery, "pageSize");
        await AssertValidationProblemAsync(fromMiddleware, "X-Time-Zone");
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string expectedField)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(
            ExpectedTopLevel.Order(StringComparer.Ordinal),
            root.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(400, root.GetProperty("status").GetInt32());

        // errors is always an object: { "field": ["message", ...] }
        var errors = root.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.TryGetProperty(expectedField, out var messages), $"Missing '{expectedField}' in {errors}");
        Assert.Equal(JsonValueKind.Array, messages.ValueKind);
        Assert.All(messages.EnumerateArray(), m => Assert.Equal(JsonValueKind.String, m.ValueKind));
        Assert.NotEmpty(messages.EnumerateArray());
    }

    private static async Task<TaskDto> CreateTaskAsync(HttpClient client, DateOnly dueDate)
    {
        using var response = await client.PostAsJsonAsync(TasksRoute, new
        {
            title = "Task",
            assignee = $"user-{Guid.NewGuid():N}",
            dueDate = Date(dueDate),
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options))!;
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
