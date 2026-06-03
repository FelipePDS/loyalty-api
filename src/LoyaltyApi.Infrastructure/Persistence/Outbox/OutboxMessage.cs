namespace LoyaltyApi.Infrastructure.Persistence.Outbox;

/// <summary>
/// Persisted record of a domain event that must be published reliably.
/// The outbox processor reads unprocessed messages and dispatches them,
/// then stamps <see cref="ProcessedAt"/> to mark completion.
/// </summary>
public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAt = DateTime.UtcNow,
            ProcessedAt = null
        };

    public Guid Id { get; private set; }

    /// <summary>Assembly-qualified type name of the original domain event.</summary>
    public string Type { get; private set; } = default!;

    /// <summary>JSON-serialized domain event payload.</summary>
    public string Payload { get; private set; } = default!;

    public DateTime OccurredAt { get; private set; }

    /// <summary>Null until the outbox processor has successfully handled the message.</summary>
    public DateTime? ProcessedAt { get; private set; }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;
}
