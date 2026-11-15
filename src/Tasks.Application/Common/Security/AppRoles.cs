namespace Tasks.Application.Common.Security;

public static class AppRoles
{
    public const string Admin = "Admin";

    public static IReadOnlyCollection<string> All { get; } = [Admin];
}
