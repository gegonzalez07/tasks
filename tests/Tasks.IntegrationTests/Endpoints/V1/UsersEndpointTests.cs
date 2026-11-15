using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tasks.Application.Common.Pagination;
using Tasks.Application.Users;
using Tasks.IntegrationTests.Infrastructure;

namespace Tasks.IntegrationTests.Endpoints.V1;

[Collection(ApiCollection.Name)]
public sealed class UsersEndpointTests(TasksApiFactory factory)
{
    private const string UsersRoute = "/api/v1/users";

    [Fact]
    public async Task GetUsers_WithoutToken_Returns401ProblemDetails()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(UsersRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetUsers_WithTamperedToken_Returns401()
    {
        using var client = factory.CreateClient();
        var token = await client.GetAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");

        using var response = await client.GetAsync(UsersRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsPagedResultContainingTheAdministrator()
    {
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();

        using var response = await client.GetAsync($"{UsersRoute}?pageNumber=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<UserDto>>();
        Assert.NotNull(page);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(5, page.PageSize);
        Assert.True(page.TotalCount >= 1);
        Assert.Contains(page.Items, u => u.UserName == TasksApiFactory.AdminUserName && u.Email == TasksApiFactory.AdminEmail);
    }

    [Fact]
    public async Task GetUsers_WithSearch_FiltersResults()
    {
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();

        using var response = await client.GetAsync($"{UsersRoute}?search=does-not-exist");

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<UserDto>>();
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=1000")]
    [InlineData("pageNumber=0")]
    public async Task GetUsers_WithInvalidPagination_Returns400(string query)
    {
        using var client = await factory.CreateClient().AuthenticateAsAdminAsync();

        using var response = await client.GetAsync($"{UsersRoute}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
