namespace LoyaltyApi.Application.Interfaces.Services;

public record IdentityUserInfo(string UserId, string Email, string Role);

public interface IIdentityService
{
    /// <summary>Creates an Identity user with the given email, password, and role. Returns the user ID on success.</summary>
    Task<(bool Succeeded, string? UserId, string? Error)> CreateUserAsync(
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user by email and password. Returns user info on success.</summary>
    Task<(bool Succeeded, IdentityUserInfo? UserInfo, string? Error)> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the primary role of a user, or null if not found.</summary>
    Task<string?> GetRoleByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
