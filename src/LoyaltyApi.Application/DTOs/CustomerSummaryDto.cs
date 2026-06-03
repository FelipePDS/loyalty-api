using LoyaltyApi.Domain.Enums;

namespace LoyaltyApi.Application.DTOs;

public sealed record CustomerSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    CustomerTier Tier,
    int PointsBalance,
    DateTime CreatedAt);
