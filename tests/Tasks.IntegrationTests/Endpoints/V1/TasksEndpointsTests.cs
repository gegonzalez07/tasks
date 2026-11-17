using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tasks.Application.Common.Pagination;
using Tasks.Application.TaskItems;
using Tasks.Domain.TaskItems;
using Tasks.IntegrationTests.Infrastructure;

namespace Tasks.IntegrationTests.Endpoints.V1;

/// <summary>
/// End-to-end CRUD over HTTP. The database is shared by the collection, so every test uses its own
/// assignee (a fresh GUID) to stay independent of the others.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TasksEndpointsTests(TasksApiFactory factory)
{
    private const string TasksRoute = "/api/v1/tasks";
    // Same "today" the API uses (frozen clock of the factory), so a date change mid-run cannot break the tests.
    private DateOnly Today => factory.UtcToday;

    [Fact]
    public async Task AllTaskEndpoints_WithoutToken_Return401()
    {
        using var client = factory.CreateClient();
        var id = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(TasksRoute)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{TasksRoute}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(TasksRoute, NewTask("x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync($"{TasksRoute}/{id}", NewTask("x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"{TasksRoute}/{id}")).StatusCode);
    }

    [Fact]
    public async Task Create_ValidTask_Returns201WithLocationAndBody()
    {
        using var client = await AuthenticatedClientAsync();
        var assignee = UniqueAssignee();

        using var response = await client.PostAsJsonAsync(TasksRoute, NewTask(assignee, status: "InProgress"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options);
        Assert.NotNull(created);
        Assert.Equal($"{TasksRoute}/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Write the README", created.Title);
        Assert.Equal(assignee, created.Assignee);
        Assert.Equal(TaskItemStatus.InProgress, created.Status);
        Assert.Equal(Today.AddDays(3), created.DueDate);

        // Enums are serialized as text.
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InProgress", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Create_WithoutStatus_DefaultsToPending()
    {
        using var client = await AuthenticatedClientAsync();

        var created = await CreateAsync(client, NewTask(UniqueAssignee()));

        Assert.Equal(TaskItemStatus.Pending, created.Status);
    }

    [Fact]
    public async Task Create_InvalidData_Returns400ProblemDetailsWithFieldErrors()
    {
        using var client = await AuthenticatedClientAsync();

        using var response = await client.PostAsJsonAsync(TasksRoute, new
        {
            title = "",
            assignee = "",
            dueDate = Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("title", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assignee", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dueDate", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_UnknownStatus_Returns400()
    {
        using var client = await AuthenticatedClientAsync();

        using var response = await client.PostAsJsonAsync(TasksRoute, NewTask(UniqueAssignee(), status: "Blocked"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingTask_Returns200()
    {
        using var client = await AuthenticatedClientAsync();
        var created = await CreateAsync(client, NewTask(UniqueAssignee()));

        var task = await client.GetFromJsonAsync<TaskDto>($"{TasksRoute}/{created.Id}", ApiJson.Options);

        Assert.NotNull(task);
        Assert.Equal(created.Id, task.Id);
        Assert.Equal(created.Title, task.Title);
    }

    [Fact]
    public async Task GetById_UnknownOrMalformedId_Returns404()
    {
        using var client = await AuthenticatedClientAsync();

        using var unknown = await client.GetAsync($"{TasksRoute}/{Guid.CreateVersion7()}");
        using var malformed = await client.GetAsync($"{TasksRoute}/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("application/problem+json", unknown.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatusAndAssigneeWithPagination()
    {
        using var client = await AuthenticatedClientAsync();
        var assignee = UniqueAssignee();
        await CreateAsync(client, NewTask(assignee, title: "Third", status: "Pending", dueInDays: 9));
        await CreateAsync(client, NewTask(assignee, title: "First", status: "Pending", dueInDays: 1));
        await CreateAsync(client, NewTask(assignee, title: "Done one", status: "Done", dueInDays: 2));
        await CreateAsync(client, NewTask(assignee, title: "Second", status: "Pending", dueInDays: 5));
        await CreateAsync(client, NewTask(UniqueAssignee(), title: "Someone else", status: "Pending", dueInDays: 1));

        var firstPage = await GetPageAsync(client, $"status=Pending&assignee={assignee.ToUpperInvariant()}&pageNumber=1&pageSize=2");
        var secondPage = await GetPageAsync(client, $"status=Pending&assignee={assignee}&pageNumber=2&pageSize=2");

        Assert.Equal(new[] { "First", "Second" }, firstPage.Items.Select(t => t.Title));
        Assert.Equal(new[] { "Third" }, secondPage.Items.Select(t => t.Title));
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.True(firstPage.HasNextPage);
        Assert.False(secondPage.HasNextPage);
    }

    [Theory]
    [InlineData("status=Blocked")]
    [InlineData("pageSize=0")]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=101")]
    public async Task List_InvalidQuery_Returns400(string query)
    {
        using var client = await AuthenticatedClientAsync();

        using var response = await client.GetAsync($"{TasksRoute}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingTask_ReplacesFields()
    {
        using var client = await AuthenticatedClientAsync();
        var created = await CreateAsync(client, NewTask(UniqueAssignee()));
        var newAssignee = UniqueAssignee();

        using var response = await client.PutAsJsonAsync(
            $"{TasksRoute}/{created.Id}",
            NewTask(newAssignee, title: "Updated", status: "Done", dueInDays: 10));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await client.GetFromJsonAsync<TaskDto>($"{TasksRoute}/{created.Id}", ApiJson.Options);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal(newAssignee, updated.Assignee);
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.Equal(Today.AddDays(10), updated.DueDate);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Update_InvalidDataOrPastDueDate_Returns400()
    {
        using var client = await AuthenticatedClientAsync();
        var created = await CreateAsync(client, NewTask(UniqueAssignee()));

        using var missingStatus = await client.PutAsJsonAsync($"{TasksRoute}/{created.Id}", NewTask("ana"));
        using var pastDueDate = await client.PutAsJsonAsync(
            $"{TasksRoute}/{created.Id}",
            NewTask("ana", status: "Pending", dueInDays: -1));

        Assert.Equal(HttpStatusCode.BadRequest, missingStatus.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, pastDueDate.StatusCode);
        Assert.Equal("application/problem+json", pastDueDate.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Update_UnknownTask_Returns404()
    {
        using var client = await AuthenticatedClientAsync();

        using var response = await client.PutAsJsonAsync(
            $"{TasksRoute}/{Guid.CreateVersion7()}",
            NewTask("ana", status: "Pending"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204ThenTaskIsGone()
    {
        using var client = await AuthenticatedClientAsync();
        var assignee = UniqueAssignee();
        var created = await CreateAsync(client, NewTask(assignee));

        using var delete = await client.DeleteAsync($"{TasksRoute}/{created.Id}");
        using var get = await client.GetAsync($"{TasksRoute}/{created.Id}");
        using var deleteAgain = await client.DeleteAsync($"{TasksRoute}/{created.Id}");
        var list = await GetPageAsync(client, $"assignee={assignee}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
        Assert.Empty(list.Items);
    }

    private async Task<HttpClient> AuthenticatedClientAsync() => await factory.CreateClient().AuthenticateAsAdminAsync();

    private static string UniqueAssignee() => $"user-{Guid.NewGuid():N}";

    private object NewTask(string assignee, string title = "Write the README", string? status = null, int dueInDays = 3) => new
    {
        title,
        description = "How to run it, decisions and AI usage.",
        assignee,
        status,
        dueDate = Today.AddDays(dueInDays).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
    };

    private static async Task<TaskDto> CreateAsync(HttpClient client, object task)
    {
        using var response = await client.PostAsJsonAsync(TasksRoute, task);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options))!;
    }

    private static async Task<PagedResponse<TaskDto>> GetPageAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync($"{TasksRoute}?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<TaskDto>>(ApiJson.Options))!;
    }
}
