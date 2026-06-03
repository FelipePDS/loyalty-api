using LoyaltyApi.Domain.Entities;

namespace LoyaltyApi.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Revokes the token identified by <paramref name="tokenHash"/>.</summary>
    Task RevokeAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Revokes all active refresh tokens for the given Identity user ID.</summary>
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
