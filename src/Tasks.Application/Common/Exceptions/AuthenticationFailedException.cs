namespace Tasks.Application.Common.Exceptions;

/// <summary>
/// Credentials were rejected. The message is intentionally generic to avoid user enumeration.
/// Mapped to HTTP 401 ProblemDetails by the API.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Invalid username or password.")
    {
    }

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
