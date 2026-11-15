namespace Tasks.Application.Auth.Abstractions;

/// <summary>User whose credentials were successfully validated.</summary>
public sealed record AuthenticatedUser(Guid Id, string UserName, string? Email, IReadOnlyCollection<string> Roles);
