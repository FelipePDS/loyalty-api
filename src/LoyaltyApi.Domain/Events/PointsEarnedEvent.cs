using LoyaltyApi.Domain.Common;

namespace LoyaltyApi.Domain.Events;

public sealed class PointsEarnedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public Guid CustomerId { get; }
    public int Points { get; }
    public Guid TransactionId { get; }

    public PointsEarnedEvent(Guid customerId, int points, Guid transactionId)
    {
        CustomerId = customerId;
        Points = points;
        TransactionId = transactionId;
    }
}
