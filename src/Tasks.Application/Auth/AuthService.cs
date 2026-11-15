using Microsoft.Extensions.Logging;
using Tasks.Application.Auth.Abstractions;
using Tasks.Application.Auth.Dtos;
using Tasks.Application.Common.Exceptions;

namespace Tasks.Application.Auth;

/// <summary>
/// Authentication use case: validates credentials against the identity store and issues a bearer token.
/// Depends only on ports (<see cref="IIdentityService"/>, <see cref="ITokenIssuer"/>), so it is fully unit-testable.
/// </summary>
public sealed class AuthService(
    IIdentityService identityService,
    ITokenIssuer tokenIssuer,
    ILogger<AuthService> logger) : IAuthService
{
    public const string BearerTokenType = "Bearer";

    public async Task<TokenResponse> AuthenticateAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            throw new AuthenticationFailedException();
        }

        var user = await identityService
            .ValidateCredentialsAsync(request.UserName.Trim(), request.Password, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("Authentication failed for user {UserName}", request.UserName);
            throw new AuthenticationFailedException();
        }

        var token = await tokenIssuer.IssueAsync(user, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Access token issued for user {UserId}, expires at {ExpiresAt}", user.Id, token.ExpiresAt);

        return new TokenResponse(token.AccessToken, BearerTokenType, token.ExpiresIn, token.ExpiresAt);
    }
}
