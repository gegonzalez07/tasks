namespace Tasks.Application.Auth.Dtos;

/// <summary>Credentials to obtain an access token.</summary>
public sealed class TokenRequest
{
    /// <summary>User name or e-mail address.</summary>
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
