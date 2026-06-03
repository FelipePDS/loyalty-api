namespace LoyaltyApi.Application.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid CustomerId,
    string Email,
    string Role);
