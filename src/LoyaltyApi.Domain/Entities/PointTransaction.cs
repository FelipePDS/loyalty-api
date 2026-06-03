using LoyaltyApi.Domain.Common;
using LoyaltyApi.Domain.Enums;

namespace LoyaltyApi.Domain.Entities;

/// <summary>
/// Immutable audit record of every point movement for a customer.
/// Positive <see cref="Points"/> = credit (Earned, positive Adjusted).
/// Negative <see cref="Points"/> = debit (Redeemed, Expired, negative Adjusted).
///
/// Factory methods are internal — only <see cref="Customer"/> may create transactions.
/// </summary>
public sealed class PointTransaction : BaseEntity<Guid>
{
    // Private parameterless constructor for EF Core.
    private PointTransaction() { }

    private PointTransaction(
        Guid id,
        Guid customerId,
        int points,
        TransactionType type,
        string description,
        string? referenceId,
        DateTime? expiresAt)
        : base(id)
    {
        CustomerId = customerId;
        Points = points;
        Type = type;
        Description = description;
        ReferenceId = referenceId;
        ExpiresAt = expiresAt;
    }

    public Guid CustomerId { get; private set; }

    /// <summary>Positive = credit; negative = debit.</summary>
    public int Points { get; private set; }

    public TransactionType Type { get; private set; }
    public string Description { get; private set; } = default!;

    /// <summary>Optional external reference, e.g., an order ID from a partner system.</summary>
    public string? ReferenceId { get; private set; }

    /// <summary>
    /// When set, indicates this earned credit will expire on that date.
    /// Null for debits and non-expiring credits.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>Indicates whether this transaction has been reversed.</summary>
    public bool IsReversed { get; private set; }

    /// <summary>The ID of the reversal transaction that reversed this one.</summary>
    public Guid? ReversedByTransactionId { get; private set; }

    /// <summary>
    /// A transaction can be reversed if it hasn't been reversed already
    /// and is of type Earned or Redeemed (not Expired, Adjusted, or Reversed).
    /// </summary>
    public bool CanBeReversed => !IsReversed && Type is TransactionType.Earned or TransactionType.Redeemed;

    // -------------------------------------------------------------------------
    // Internal factory methods — only Customer (same assembly) may call these.
    // -------------------------------------------------------------------------

    internal static PointTransaction CreateEarned(
        Guid id,
        Guid customerId,
        int points,
        string description,
        string? referenceId,
        DateTime? expiresAt)
        => new(id, customerId, points, TransactionType.Earned, description, referenceId, expiresAt);

    internal static PointTransaction CreateRedeemed(
        Guid id,
        Guid customerId,
        int points,
        string description)
        => new(id, customerId, -points, TransactionType.Redeemed, description, null, null);

    internal static PointTransaction CreateExpired(
        Guid id,
        Guid customerId,
        int points,
        string description)
        => new(id, customerId, -points, TransactionType.Expired, description, null, null);

    internal static PointTransaction CreateAdjusted(
        Guid id,
        Guid customerId,
        int points,
        string description)
        => new(id, customerId, points, TransactionType.Adjusted, description, null, null);

    internal static PointTransaction CreateReversed(
        Guid id,
        Guid customerId,
        int points,
        string description,
        string? referenceId)
        => new(id, customerId, points, TransactionType.Reversed, description, referenceId, null);

    /// <summary>Marks this transaction as reversed by the given reversal transaction.</summary>
    internal void MarkAsReversed(Guid reversalTransactionId)
    {
        IsReversed = true;
        ReversedByTransactionId = reversalTransactionId;
        UpdatedAt = DateTime.UtcNow;
    }
}
