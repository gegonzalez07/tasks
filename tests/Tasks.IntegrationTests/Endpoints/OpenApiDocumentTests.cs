using System.Net;
using Tasks.IntegrationTests.Infrastructure;

namespace Tasks.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
public sealed class OpenApiDocumentTests(TasksApiFactory factory)
{
    [Fact]
    public async Task SwaggerV1_DocumentsEveryVersionedEndpoint()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/auth/token", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/users", document, StringComparison.Ordinal);
        Assert.Contains("Obtain an access token", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/tasks", document, StringComparison.Ordinal);
        Assert.Contains("/api/v1/tasks/{id}", document, StringComparison.Ordinal);
        Assert.Contains("Create a task", document, StringComparison.Ordinal);
    }
}
