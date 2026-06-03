using LoyaltyApi.Domain.Common;
using LoyaltyApi.Domain.Enums;
using LoyaltyApi.Domain.Events;
using LoyaltyApi.Domain.Exceptions;
using LoyaltyApi.Domain.ValueObjects;

namespace LoyaltyApi.Domain.Entities;

/// <summary>
/// Customer aggregate root. All point balance mutations must go through this class.
/// Invariants enforced here:
///   - PointsBalance is never negative.
///   - Every balance change produces an immutable PointTransaction.
///   - Tier upgrades are automatic and raise a domain event.
/// </summary>
public sealed class Customer : BaseEntity<Guid>
{
    // Private parameterless constructor for EF Core.
    private Customer() { }

    private Customer(Guid id, string fullName, Email email, Document document)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        Document = document;
        Tier = CustomerTier.Standard;
        PointsBalance = 0;
        TotalPointsEarned = 0;
    }

    public string FullName { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public Document Document { get; private set; } = default!;
    public CustomerTier Tier { get; private set; }

    /// <summary>Current redeemable balance. Never goes below zero.</summary>
    public int PointsBalance { get; private set; }

    /// <summary>
    /// Cumulative lifetime earned points. Used exclusively for tier upgrade evaluation.
    /// Never decremented — redemptions and expirations do not affect it.
    /// </summary>
    public int TotalPointsEarned { get; private set; }

    // -------------------------------------------------------------------------
    // Factory
    // -------------------------------------------------------------------------

    public static Customer Create(string fullName, Email email, Document document)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name cannot be empty.");

        return new Customer(Guid.NewGuid(), fullName, email, document);
    }

    // -------------------------------------------------------------------------
    // Domain behaviour
    // -------------------------------------------------------------------------

    /// <summary>
    /// Credits points to the customer's balance, creates an audit transaction,
    /// evaluates tier upgrade, and raises <see cref="PointsEarnedEvent"/>.
    /// </summary>
    /// <returns>The newly created <see cref="PointTransaction"/> (caller must persist it).</returns>
    public PointTransaction EarnPoints(
        int points,
        string description,
        string? referenceId = null,
        DateTime? expiresAt = null)
    {
        if (points <= 0)
            throw new DomainException("Points to earn must be positive.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");

        var transactionId = Guid.NewGuid();
        var transaction = PointTransaction.CreateEarned(transactionId, Id, points, description, referenceId, expiresAt);

        PointsBalance += points;
        TotalPointsEarned += points;
        UpdatedAt = DateTime.UtcNow;

        UpgradeTierIfEligible();

        AddDomainEvent(new PointsEarnedEvent(Id, points, transactionId));

        return transaction;
    }

    /// <summary>
    /// Debits points from the customer's balance.
    /// Throws <see cref="InsufficientPointsException"/> if balance is insufficient.
    /// </summary>
    /// <returns>The newly created <see cref="PointTransaction"/> (caller must persist it).</returns>
    public PointTransaction RedeemPoints(int points, string description)
    {
        if (points <= 0)
            throw new DomainException("Points to redeem must be positive.");

        if (PointsBalance < points)
            throw new InsufficientPointsException(PointsBalance, points);

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");

        var transactionId = Guid.NewGuid();
        var transaction = PointTransaction.CreateRedeemed(transactionId, Id, points, description);

        PointsBalance -= points;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PointsRedeemedEvent(Id, points, transactionId));

        return transaction;
    }

    /// <summary>
    /// Expires up to <paramref name="points"/> from the customer's balance, flooring at zero.
    /// The actual deducted amount may be less than <paramref name="points"/> when the balance
    /// is already lower than the expiring amount.
    /// </summary>
    /// <returns>The newly created <see cref="PointTransaction"/> (caller must persist it).</returns>
    /// <exception cref="DomainException">Thrown when PointsBalance is already zero.</exception>
    public PointTransaction ExpirePoints(int points)
    {
        if (points <= 0)
            throw new DomainException("Points to expire must be positive.");

        if (PointsBalance == 0)
            throw new DomainException("Cannot expire points when the balance is already zero.");

        var actualExpired = Math.Min(points, PointsBalance);
        var transactionId = Guid.NewGuid();
        var transaction = PointTransaction.CreateExpired(
            transactionId, Id, actualExpired, "Points expired due to campaign expiration.");

        PointsBalance = Math.Max(0, PointsBalance - points);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PointsExpiredEvent(Id, actualExpired, 1));

        return transaction;
    }

    /// <summary>
    /// Admin-only manual adjustment. Accepts positive (credit) or negative (debit) values.
    /// A negative adjustment cannot drive the balance below zero.
    /// </summary>
    /// <returns>The newly created <see cref="PointTransaction"/> (caller must persist it).</returns>
    public PointTransaction AdjustPoints(int points, string description)
    {
        if (points == 0)
            throw new DomainException("Adjustment amount cannot be zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");

        if (PointsBalance + points < 0)
            throw new DomainException(
                $"Adjustment would result in a negative balance. Current balance: {PointsBalance}.");

        var transactionId = Guid.NewGuid();
        var transaction = PointTransaction.CreateAdjusted(transactionId, Id, points, description);

        PointsBalance += points;
        UpdatedAt = DateTime.UtcNow;

        return transaction;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void UpgradeTierIfEligible()
    {
        var targetTier = TotalPointsEarned switch
        {
            >= 5000 => CustomerTier.Gold,
            >= 1000 => CustomerTier.Silver,
            _ => CustomerTier.Standard
        };

        if (targetTier > Tier)
        {
            var previousTier = Tier;
            Tier = targetTier;
            AddDomainEvent(new CustomerTierUpgradedEvent(Id, previousTier, Tier));
        }
    }
}
