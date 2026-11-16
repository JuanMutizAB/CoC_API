namespace CoC_API.DTOs.Request;

public record LoginRequest(
    string Email,
    string Password
);
