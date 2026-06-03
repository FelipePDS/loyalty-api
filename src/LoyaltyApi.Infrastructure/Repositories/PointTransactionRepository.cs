using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.Enums;
using LoyaltyApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyApi.Infrastructure.Repositories;

internal sealed class PointTransactionRepository : IPointTransactionRepository
{
    private readonly LoyaltyApiDbContext _context;

    public PointTransactionRepository(LoyaltyApiDbContext context) => _context = context;

    public async Task<(IReadOnlyList<PointTransaction> Items, int TotalCount)> GetByCustomerIdAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PointTransactions
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(PointTransaction transaction, CancellationToken cancellationToken = default)
        => await _context.PointTransactions.AddAsync(transaction, cancellationToken);

    public void Update(PointTransaction transaction)
        => _context.PointTransactions.Update(transaction);

    public async Task<PointTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.PointTransactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PointTransaction>> GetExpiredUnprocessedAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
        => await _context.PointTransactions
            .AsNoTracking()
            .Where(t =>
                t.Type == TransactionType.Earned &&
                t.ExpiresAt != null &&
                t.ExpiresAt <= cutoff &&
                t.Points > 0)
            .OrderBy(t => t.ExpiresAt)
            .ToListAsync(cancellationToken);

    public async Task<int> GetAboutToExpirePointsAsync(
        Guid customerId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
        => await _context.PointTransactions
            .AsNoTracking()
            .Where(t =>
                t.CustomerId == customerId &&
                t.Type == TransactionType.Earned &&
                t.ExpiresAt != null &&
                t.ExpiresAt > from &&
                t.ExpiresAt <= to)
            .SumAsync(t => t.Points, cancellationToken);
}
