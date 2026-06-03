using LoyaltyApi.Domain.Common;

namespace LoyaltyApi.Domain.Events;

/// <summary>
/// Raised when a batch of expired points is deducted from a customer's balance.
/// <paramref name="Points"/> is the total points deducted; <paramref name="ExpiredCount"/>
/// is the number of expiration records that triggered this deduction.
/// </summary>
public sealed class PointsExpiredEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public Guid CustomerId { get; }
    public int Points { get; }
    public int ExpiredCount { get; }

    public PointsExpiredEvent(Guid customerId, int points, int expiredCount)
    {
        CustomerId = customerId;
        Points = points;
        ExpiredCount = expiredCount;
    }
}
