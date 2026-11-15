namespace Tasks.Domain.Exceptions;

/// <summary>
/// Base type for errors raised by business rules. Mapped to RFC 9457 ProblemDetails by the API.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Stable, machine readable error code (e.g. <c>tasks.not_found</c>).</summary>
    public abstract string ErrorCode { get; }
}
