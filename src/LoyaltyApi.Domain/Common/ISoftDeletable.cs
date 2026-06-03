namespace LoyaltyApi.Domain.Common;

/// <summary>
/// Marks an entity as soft-deletable. EF Core global query filters will exclude
/// rows where <see cref="IsDeleted"/> is <c>true</c>.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    void SoftDelete();
}
