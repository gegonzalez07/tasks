namespace Tasks.Api.Configuration;

/// <summary>
/// Published API versions. Every endpoint must call <c>Version(n)</c>; adding a new version here
/// creates its own OpenAPI document (<c>/swagger/v{n}/swagger.json</c>).
/// </summary>
public static class ApiVersions
{
    public const int V1 = 1;

    public static IReadOnlyList<int> Published { get; } = [V1];
}
