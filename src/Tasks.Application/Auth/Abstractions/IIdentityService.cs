namespace Tasks.Application.Auth.Abstractions;

/// <summary>
/// Port to the identity store (implemented with ASP.NET Core Identity in Infrastructure).
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Validates the credentials (honouring lockout and active status).
    /// Returns <c>null</c> when they are not valid for any reason.
    /// </summary>
    Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default);
}
