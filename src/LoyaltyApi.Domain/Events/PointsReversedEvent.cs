using LoyaltyApi.Domain.Common;

namespace LoyaltyApi.Domain.Events;

public sealed class PointsReversedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public Guid CustomerId { get; }
    public int Points { get; }
    public Guid ReversalTransactionId { get; }
    public Guid OriginalTransactionId { get; }

    public PointsReversedEvent(Guid customerId, int points, Guid reversalTransactionId, Guid originalTransactionId)
    {
        CustomerId = customerId;
        Points = points;
        ReversalTransactionId = reversalTransactionId;
        OriginalTransactionId = originalTransactionId;
    }
}
