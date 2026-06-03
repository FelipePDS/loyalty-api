using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LoyaltyApi.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LoyaltyApi.Infrastructure.Services;

/// <summary>
/// Generates and validates JWT access tokens and cryptographically random refresh tokens.
///
/// JWT claims included: sub, email, role, jti, iat, exp, iss, aud.
/// Refresh tokens are 64 random bytes encoded as Base64 (raw) with a SHA-256 hash stored in DB.
/// </summary>
internal sealed class TokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

        _accessTokenExpirationMinutes = int.Parse(
            configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        _refreshTokenExpirationDays = int.Parse(
            configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");

        if (secretKey.Length < 32)
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters.");

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var allClaims = claims.ToList();

        // Ensure jti is present for token uniqueness / revocation tracking.
        if (!allClaims.Any(c => c.Type == JwtRegisteredClaimNames.Jti))
            allClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: allClaims,
            notBefore: now,
            expires: now.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public TokenPair GenerateRefreshToken()
    {
        // 64 cryptographically random bytes — sufficient entropy for a refresh token.
        var rawBytes = new byte[64];
        RandomNumberGenerator.Fill(rawBytes);
        var rawToken = Convert.ToBase64String(rawBytes);
        var hashedToken = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

        return new TokenPair(
            AccessToken: string.Empty, // filled by caller who also has the access token
            RawRefreshToken: rawToken,
            HashedRefreshToken: hashedToken,
            RefreshTokenExpiresAt: expiresAt);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = false, // Intentionally skip lifetime check for refresh flow
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Produces a hex-encoded SHA-256 hash of the raw refresh token.</summary>
    public static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
