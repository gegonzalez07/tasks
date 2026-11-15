namespace Tasks.Infrastructure.Options;

public enum DatabaseInitializationStrategy
{
    /// <summary>Applies EF Core migrations (falls back to EnsureCreated when none exist yet).</summary>
    Migrate,

    /// <summary>Creates the schema from the model without migrations (tests / prototypes).</summary>
    EnsureCreated,

    /// <summary>Does not touch the schema; only seeds data.</summary>
    None,
}

/// <summary>Database settings (section <c>Database</c>).</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    public DatabaseInitializationStrategy InitializationStrategy { get; set; } = DatabaseInitializationStrategy.Migrate;

    public int MaxRetryCount { get; set; } = 5;
}
