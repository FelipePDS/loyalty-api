namespace LoyaltyApi.Domain.Common;

/// <summary>
/// Base class for all domain entities. Owns the domain event collection so aggregate roots
/// can raise events without depending on any external library.
/// </summary>
public abstract class BaseEntity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>EF Core requires a parameterless constructor (can be private/protected).</summary>
    protected BaseEntity() { }

    protected BaseEntity(TId id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public TId Id { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    /// <summary>Protected setter so derived entities can stamp UpdatedAt on state-changing methods.</summary>
    public DateTime UpdatedAt { get; protected set; }

    public IReadOnlyList<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
