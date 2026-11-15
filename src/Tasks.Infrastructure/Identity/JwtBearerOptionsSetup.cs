using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tasks.Application.Common.Security;
using Tasks.Infrastructure.Options;

namespace Tasks.Infrastructure.Identity;

/// <summary>
/// Configures the API (resource server) to validate the bearer tokens issued by the embedded IdentityServer.
/// Signing keys are read in-process from IdentityServer's key material, so no HTTP call to the
/// discovery document is needed (works identically in tests and behind proxies).
/// </summary>
internal sealed class JwtBearerOptionsSetup(
    IOptions<AuthenticationSettings> settings,
    IServiceScopeFactory scopeFactory) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name == JwtBearerDefaults.AuthenticationScheme)
        {
            Configure(options);
        }
    }

    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var authentication = settings.Value;

        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authentication.Issuer,
            ValidateAudience = true,
            ValidAudience = authentication.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.Name,
            RoleClaimType = AppClaimTypes.Role,
            IssuerSigningKeyResolver = (_, _, _, _) => GetValidationKeys(),
        };
    }

    private List<SecurityKey> GetValidationKeys()
    {
        using var scope = scopeFactory.CreateScope();
        var keyMaterial = scope.ServiceProvider.GetRequiredService<IKeyMaterialService>();
        var keys = keyMaterial.GetValidationKeysAsync().GetAwaiter().GetResult();
        return keys.Select(k => k.Key).ToList();
    }
}
