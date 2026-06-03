using LoyaltyApi.Domain.Common;
using LoyaltyApi.Domain.Exceptions;

namespace LoyaltyApi.Domain.Entities;

/// <summary>
/// Represents a time-bounded marketing campaign that governs how many points
/// are issued per unit of value spent.
/// </summary>
public sealed class Campaign : BaseEntity<Guid>
{
    // Private parameterless constructor for EF Core.
    private Campaign() { }

    private Campaign(Guid id, string name, decimal pointsPerUnit, DateTime startDate, DateTime endDate)
        : base(id)
    {
        Name = name;
        PointsPerUnit = pointsPerUnit;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
    }

    public string Name { get; private set; } = default!;
    public decimal PointsPerUnit { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsActive { get; private set; }

    public static Campaign Create(string name, decimal pointsPerUnit, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Campaign name cannot be empty.");

        if (pointsPerUnit <= 0)
            throw new DomainException("Points per unit must be greater than zero.");

        if (startDate >= endDate)
            throw new DomainException("Campaign start date must be before end date.");

        return new Campaign(Guid.NewGuid(), name, pointsPerUnit, startDate, endDate);
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Campaign is already inactive.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            throw new DomainException("Campaign is already active.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
