using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tasks.Application.Auth.Dtos;

namespace Tasks.IntegrationTests.Infrastructure;

internal static class HttpClientExtensions
{
    public const string TokenRoute = "/api/v1/auth/token";

    public static async Task<HttpResponseMessage> RequestTokenAsync(this HttpClient client, string userName, string password) =>
        await client.PostAsJsonAsync(TokenRoute, new TokenRequest { UserName = userName, Password = password });

    public static async Task<string> GetAdminTokenAsync(this HttpClient client)
    {
        using var response = await client.RequestTokenAsync(TasksApiFactory.AdminUserName, TasksApiFactory.AdminPassword);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return token!.AccessToken;
    }

    public static async Task<HttpClient> AuthenticateAsAdminAsync(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await client.GetAdminTokenAsync());
        return client;
    }
}
