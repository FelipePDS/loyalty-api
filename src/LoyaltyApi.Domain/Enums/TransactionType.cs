namespace LoyaltyApi.Domain.Enums;

/// <summary>
/// Describes the direction and reason for a point movement.
/// Earned / Adjusted (positive) → credit; Redeemed / Expired (negative) → debit.
/// </summary>
public enum TransactionType
{
    Earned = 0,
    Redeemed = 1,
    Expired = 2,
    Adjusted = 3,
    Reversed = 4
}
