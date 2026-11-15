namespace Tasks.Application.Common.Security;

/// <summary>JWT claim names (short form). Inbound claim mapping is disabled in the API.</summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
}
