namespace dashboardapi.DTOs;

public record UserDto(
    string UserId,
    string Email,
    string FullName,
    string Role,
    string Status,
    IReadOnlyCollection<string>? ProjectIds = null);

public record UserCreateDto(string Email, string FullName, string Role, string Password);

public record UserUpdateDto(
    string? Role = null,
    string? Status = null,
    string? Email = null,
    string? FullName = null,
    string? Password = null,
    IReadOnlyCollection<string>? ProjectIds = null);
