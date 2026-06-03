namespace LoyaltyApi.Domain.Enums;

/// <summary>
/// Customer loyalty tier. Ordering is significant — higher value = better tier.
/// Upgrade thresholds: Silver at 1,000 total earned points; Gold at 5,000.
/// </summary>
public enum CustomerTier
{
    Standard = 0,
    Silver = 1,
    Gold = 2
}
