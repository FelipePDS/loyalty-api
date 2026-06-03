using System.Text.Json;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Domain.Common;
using LoyaltyApi.Infrastructure.Persistence;
using LoyaltyApi.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoyaltyApi.Infrastructure.Persistence;

/// <summary>
/// Wraps <see cref="LoyaltyApiDbContext"/> to provide a clean unit-of-work abstraction.
///
/// Before committing, all domain events collected on tracked aggregates are:
///   1. Serialized and written to <see cref="OutboxMessage"/> for reliable async processing.
///   2. Cleared from the aggregate so they are not re-published on subsequent saves.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly LoyaltyApiDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(LoyaltyApiDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PublishDomainEventsToOutbox();
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            PublishDomainEventsToOutbox();
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
            await _currentTransaction.DisposeAsync();
    }

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------

    private void PublishDomainEventsToOutbox()
    {
        // Find all tracked aggregates that have pending domain events.
        var aggregates = _context.ChangeTracker
            .Entries<BaseEntity<Guid>>()
            .Where(e => e.Entity.GetDomainEvents().Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.GetDomainEvents())
            {
                var typeName = domainEvent.GetType().AssemblyQualifiedName
                               ?? domainEvent.GetType().FullName
                               ?? domainEvent.GetType().Name;

                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

                _context.OutboxMessages.Add(OutboxMessage.Create(typeName, payload));
            }

            aggregate.ClearDomainEvents();
        }
    }
}
