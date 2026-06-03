using LoyaltyApi.Domain.Entities;

namespace LoyaltyApi.Application.Interfaces.Repositories;

public interface IPointTransactionRepository
{
    Task<PointTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated, ordered (newest-first) list of transactions for a customer.</summary>
    Task<(IReadOnlyList<PointTransaction> Items, int TotalCount)> GetByCustomerIdAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(PointTransaction transaction, CancellationToken cancellationToken = default);

    void Update(PointTransaction transaction);

    /// <summary>
    /// Returns transactions of type Earned with a non-null ExpiresAt that is &lt;= <paramref name="cutoff"/>
    /// and whose corresponding customer still has a positive balance.
    /// Used exclusively by the expiration background service.
    /// </summary>
    Task<IReadOnlyList<PointTransaction>> GetExpiredUnprocessedAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the sum of points from Earned transactions whose ExpiresAt is between <paramref name="from"/> and <paramref name="to"/>.</summary>
    Task<int> GetAboutToExpirePointsAsync(
        Guid customerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
