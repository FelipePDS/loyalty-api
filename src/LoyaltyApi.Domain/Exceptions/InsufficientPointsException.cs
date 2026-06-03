namespace LoyaltyApi.Domain.Exceptions;

/// <summary>
/// Raised when a redemption request exceeds the customer's current points balance.
/// Carries both the balance and the requested amount so the caller can surface a
/// precise error message without re-querying.
/// </summary>
public sealed class InsufficientPointsException : DomainException
{
    public int CurrentBalance { get; }
    public int RequestedPoints { get; }

    public InsufficientPointsException(int currentBalance, int requestedPoints)
        : base($"Insufficient points. Current balance: {currentBalance}, requested: {requestedPoints}.")
    {
        CurrentBalance = currentBalance;
        RequestedPoints = requestedPoints;
    }
}
