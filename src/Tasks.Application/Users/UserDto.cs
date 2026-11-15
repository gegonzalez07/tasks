namespace Tasks.Application.Users;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string? Email,
    string? FullName,
    bool IsActive,
    DateTime CreatedAt);
