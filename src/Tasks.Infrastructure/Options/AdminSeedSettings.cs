namespace Tasks.Infrastructure.Options;

/// <summary>Default administrator created at startup (section <c>Seed:Admin</c>).</summary>
public sealed class AdminSeedSettings
{
    public const string SectionName = "Seed:Admin";

    public string UserName { get; set; } = "admin";

    public string Email { get; set; } = "admin@tasks.local";

    public string FullName { get; set; } = "System Administrator";

    /// <summary>
    /// Initial password. Never commit a production value: use user-secrets or environment variables
    /// (<c>Seed__Admin__Password</c>). When empty the administrator user is not created.
    /// </summary>
    public string? Password { get; set; }
}
