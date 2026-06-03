using LoyaltyApi.Domain.Enums;

namespace LoyaltyApi.Application.DTOs;

public sealed record CustomerBalanceDto(
    Guid CustomerId,
    string FullName,
    CustomerTier Tier,
    int PointsBalance,
    int PointsAboutToExpire);
