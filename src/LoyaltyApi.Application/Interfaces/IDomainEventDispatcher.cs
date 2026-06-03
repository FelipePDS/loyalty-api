using LoyaltyApi.Domain.Common;

namespace LoyaltyApi.Application.Interfaces;

/// <summary>
/// Dispatches domain events collected on aggregate roots.
/// Implementations decide whether to handle in-process or write to the outbox.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
