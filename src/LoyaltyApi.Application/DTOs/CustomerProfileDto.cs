using LoyaltyApi.Domain.Enums;

namespace LoyaltyApi.Application.DTOs;

public sealed record CustomerProfileDto(
    Guid CustomerId,
    string FullName,
    string Email,
    CustomerTier Tier,
    int PointsBalance,
    int TotalPointsEarned,
    int PointsToNextTier,
    CustomerTier? NextTier,
    DateTime CreatedAt);
