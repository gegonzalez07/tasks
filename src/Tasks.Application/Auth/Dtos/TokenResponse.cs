namespace Tasks.Application.Auth.Dtos;

/// <summary>Bearer token issued by the embedded IdentityServer.</summary>
/// <param name="AccessToken">Signed JWT to send in the <c>Authorization: Bearer</c> header.</param>
/// <param name="TokenType">Always <c>Bearer</c>.</param>
/// <param name="ExpiresIn">Lifetime in seconds.</param>
/// <param name="ExpiresAt">Absolute UTC expiration.</param>
public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, DateTimeOffset ExpiresAt);
