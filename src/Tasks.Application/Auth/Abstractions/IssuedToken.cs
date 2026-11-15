namespace Tasks.Application.Auth.Abstractions;

public sealed record IssuedToken(string AccessToken, int ExpiresIn, DateTimeOffset ExpiresAt);
