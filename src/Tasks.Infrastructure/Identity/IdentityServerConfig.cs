using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Tasks.Application.Common.Security;
using Tasks.Infrastructure.Options;

namespace Tasks.Infrastructure.Identity;

/// <summary>In-memory IdentityServer resources, scopes and clients.</summary>
internal static class IdentityServerConfig
{
    public static IEnumerable<ApiScope> GetApiScopes(AuthenticationSettings settings) =>
    [
        new ApiScope(settings.Scope, "Tasks API"),
    ];

    public static IEnumerable<ApiResource> GetApiResources(AuthenticationSettings settings) =>
    [
        new ApiResource(settings.Audience, "Tasks API")
        {
            Scopes = { settings.Scope },
            UserClaims = { AppClaimTypes.Name, AppClaimTypes.Email, AppClaimTypes.Role },
        },
    ];

    public static IEnumerable<Client> GetClients(AuthenticationSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ClientSecret))
        {
            return [];
        }

        return
        [
            new Client
            {
                ClientId = settings.ClientId,
                ClientName = "Tasks API client",
                ClientSecrets = { new Secret(settings.ClientSecret.Sha256()) },
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                AllowedScopes = { settings.Scope, IdentityServerConstants.StandardScopes.OpenId },
                AccessTokenLifetime = settings.AccessTokenLifetimeSeconds,
                AllowOfflineAccess = false,
            },
        ];
    }

    public static IEnumerable<IdentityResource> GetIdentityResources() =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
    ];
}
