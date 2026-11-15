namespace Tasks.Domain.Exceptions;

/// <summary>The operation conflicts with the current state of a resource (e.g. duplicates).</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public override string ErrorCode => "resource.conflict";
}
