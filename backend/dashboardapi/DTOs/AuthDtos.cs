namespace dashboardapi.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string UserId, string FullName, string Role);
public record MeResponse(string UserId, string Email, string FullName, string Role, string Status);