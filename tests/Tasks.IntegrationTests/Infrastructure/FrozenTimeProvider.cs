namespace Tasks.IntegrationTests.Infrastructure;

/// <summary>A clock stuck at a fixed instant (only <see cref="GetUtcNow"/> is frozen).</summary>
internal sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
