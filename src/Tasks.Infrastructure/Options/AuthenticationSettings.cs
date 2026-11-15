namespace Tasks.Infrastructure.Options;

/// <summary>Token issuing / validation settings (section <c>Authentication</c>).</summary>
public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>Value of the <c>iss</c> claim (lower case). Must be identical for issuing and validation.</summary>
    public string Issuer { get; set; } = "https://tasks.local";

    /// <summary>Value of the <c>aud</c> claim expected by the API.</summary>
    public string Audience { get; set; } = "tasks-api";

    /// <summary>OAuth scope granted to API tokens.</summary>
    public string Scope { get; set; } = "tasks.api";

    public int AccessTokenLifetimeSeconds { get; set; } = 3600;

    /// <summary>
    /// Optional OAuth client allowed to use the standard <c>/connect/token</c> endpoint (password grant).
    /// Leave the secret empty to disable it.
    /// </summary>
    public string ClientId { get; set; } = "tasks-client";

    public string? ClientSecret { get; set; }

    /// <summary>PFX certificate used to sign tokens outside Development/Testing.</summary>
    public string? SigningCertificatePath { get; set; }

    public string? SigningCertificatePassword { get; set; }
}
