namespace Tasks.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    public string ResourceName { get; }

    public object Key { get; }

    public override string ErrorCode => "resource.not_found";
}
