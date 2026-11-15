using System.Security.Claims;
using Duende.IdentityServer;
using Microsoft.Extensions.Options;
using Tasks.Application.Auth.Abstractions;
using Tasks.Application.Common.Security;
using Tasks.Infrastructure.Options;

namespace Tasks.Infrastructure.Identity;

/// <summary>
/// Issues signed JWT access tokens through the embedded Duende IdentityServer token engine
/// (same signing keys exposed at <c>/.well-known/openid-configuration/jwks</c>).
/// </summary>
internal sealed class IdentityServerTokenIssuer(
    IIdentityServerTools tools,
    IOptions<AuthenticationSettings> settings,
    TimeProvider timeProvider) : ITokenIssuer
{
    public async Task<IssuedToken> IssueAsync(AuthenticatedUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        var options = settings.Value;
        var lifetime = options.AccessTokenLifetimeSeconds;

        var claims = new List<Claim>
        {
            new(AppClaimTypes.Subject, user.Id.ToString()),
            new(AppClaimTypes.Name, user.UserName),
            new("jti", Guid.NewGuid().ToString("N")),
            new("aud", options.Audience),
            new("scope", options.Scope),
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(AppClaimTypes.Email, user.Email));
        }

        claims.AddRange(user.Roles.Select(role => new Claim(AppClaimTypes.Role, role)));

        var issuedAt = timeProvider.GetUtcNow();
        var jwt = await tools.IssueJwtAsync(lifetime, claims).ConfigureAwait(false);

        return new IssuedToken(jwt, lifetime, issuedAt.AddSeconds(lifetime));
    }
}
