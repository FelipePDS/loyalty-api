using LoyaltyApi.Domain.Common;
using LoyaltyApi.Domain.Enums;

namespace LoyaltyApi.Domain.Events;

public sealed class CustomerTierUpgradedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public Guid CustomerId { get; }
    public CustomerTier OldTier { get; }
    public CustomerTier NewTier { get; }

    public CustomerTierUpgradedEvent(Guid customerId, CustomerTier oldTier, CustomerTier newTier)
    {
        CustomerId = customerId;
        OldTier = oldTier;
        NewTier = newTier;
    }
}
