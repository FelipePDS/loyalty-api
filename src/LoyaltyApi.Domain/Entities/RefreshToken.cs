using LoyaltyApi.Domain.Common;
using LoyaltyApi.Domain.Exceptions;

namespace LoyaltyApi.Domain.Entities;

/// <summary>
/// Stores a hashed refresh token linked to an ASP.NET Core Identity user.
/// The raw token is never persisted — only the SHA-256 hash.
/// Tokens are rotated on every use; revocation is permanent.
/// </summary>
public sealed class RefreshToken : BaseEntity<Guid>
{
    // Private parameterless constructor for EF Core.
    private RefreshToken() { }

    private RefreshToken(Guid id, string userId, string hashedToken, DateTime expiresAt)
        : base(id)
    {
        UserId = userId;
        Token = hashedToken;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    /// <summary>Foreign key to the ASP.NET Core Identity user (IdentityUser.Id).</summary>
    public string UserId { get; private set; } = default!;

    /// <summary>Cryptographic hash of the raw token. Never store or log the raw value.</summary>
    public string Token { get; private set; } = default!;

    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(string userId, string hashedToken, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(hashedToken))
            throw new DomainException("Token hash cannot be empty.");

        if (expiresAt <= DateTime.UtcNow)
            throw new DomainException("Refresh token expiration must be in the future.");

        return new RefreshToken(Guid.NewGuid(), userId, hashedToken, expiresAt);
    }

    /// <summary>Permanently revokes this token. Idempotent revocation is not allowed — callers must check <see cref="IsActive"/> first.</summary>
    public void Revoke()
    {
        if (IsRevoked)
            throw new DomainException("Refresh token is already revoked.");

        IsRevoked = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
