namespace dashboardapi.DTOs;

public record UserDto(string UserId, string Email, string FullName, string Role, string Status);
public record UserCreateDto(string Email, string FullName, string Role, string Password);
public record UserUpdateDto(string? Role, string? Status);