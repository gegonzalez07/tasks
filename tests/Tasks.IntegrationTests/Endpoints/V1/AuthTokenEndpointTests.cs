using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tasks.Application.Auth.Dtos;
using Tasks.IntegrationTests.Infrastructure;

namespace Tasks.IntegrationTests.Endpoints.V1;

[Collection(ApiCollection.Name)]
public sealed class AuthTokenEndpointTests(TasksApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostToken_WithSeededAdminCredentials_ReturnsBearerToken()
    {
        using var response = await _client.RequestTokenAsync(TasksApiFactory.AdminUserName, TasksApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));
        Assert.Equal(3, token.AccessToken.Split('.').Length); // header.payload.signature
        Assert.Equal("Bearer", token.TokenType);
        Assert.True(token.ExpiresIn > 0);
    }

    [Fact]
    public async Task PostToken_WithEmailInsteadOfUserName_ReturnsToken()
    {
        using var response = await _client.RequestTokenAsync(TasksApiFactory.AdminEmail, TasksApiFactory.AdminPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostToken_WithUnknownUser_ReturnsProblemDetails401()
    {
        using var response = await _client.RequestTokenAsync("nobody", "Wr0ng!Password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("title", out _));
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task PostToken_WithMissingFields_ReturnsValidationProblemDetails400()
    {
        using var response = await _client.PostAsJsonAsync(HttpClientExtensions.TokenRoute, new { userName = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task PostToken_WithoutVersionSegment_IsNotRouted()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/token",
            new TokenRequest { UserName = TasksApiFactory.AdminUserName, Password = TasksApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
