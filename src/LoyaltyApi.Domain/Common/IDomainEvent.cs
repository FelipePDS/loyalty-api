namespace LoyaltyApi.Domain.Common;

/// <summary>
/// Marker interface for all domain events. Implementations are raised by aggregate roots
/// and dispatched by the infrastructure layer via IDomainEventDispatcher.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
