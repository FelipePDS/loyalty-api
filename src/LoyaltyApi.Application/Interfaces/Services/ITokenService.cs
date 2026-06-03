using System.Security.Claims;

namespace LoyaltyApi.Application.Interfaces.Services;

public record TokenPair(string AccessToken, string RawRefreshToken, string HashedRefreshToken, DateTime RefreshTokenExpiresAt);

public interface ITokenService
{
    /// <summary>Generates a signed JWT access token containing the provided claims.</summary>
    string GenerateAccessToken(IEnumerable<Claim> claims);

    /// <summary>
    /// Generates a cryptographically random refresh token (64 bytes).
    /// Returns both the raw value (send to client) and its SHA-256 hash (store in DB).
    /// </summary>
    TokenPair GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT and returns its claims principal WITHOUT checking token lifetime.
    /// Used in the refresh token flow where the access token may be expired.
    /// Returns null if the token is structurally invalid or the signature is wrong.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
