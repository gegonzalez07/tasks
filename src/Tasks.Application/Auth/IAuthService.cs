using Tasks.Application.Auth.Dtos;

namespace Tasks.Application.Auth;

public interface IAuthService
{
    /// <summary>
    /// Exchanges credentials for a bearer token.
    /// </summary>
    /// <exception cref="Common.Exceptions.AuthenticationFailedException">The credentials are not valid.</exception>
    Task<TokenResponse> AuthenticateAsync(TokenRequest request, CancellationToken cancellationToken = default);
}
