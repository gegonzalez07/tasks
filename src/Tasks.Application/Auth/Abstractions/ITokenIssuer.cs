namespace Tasks.Application.Auth.Abstractions;

/// <summary>
/// Port to the token authority (implemented with Duende IdentityServer in Infrastructure).
/// </summary>
public interface ITokenIssuer
{
    Task<IssuedToken> IssueAsync(AuthenticatedUser user, CancellationToken cancellationToken = default);
}
