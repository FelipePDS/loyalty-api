namespace LoyaltyApi.Application.Interfaces;

/// <summary>
/// Abstracts the DB transaction lifecycle so command handlers can be wrapped
/// in a transaction without depending on EF Core directly.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
