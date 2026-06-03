namespace LoyaltyApi.Application.DTOs;

public sealed record CampaignDto(
    Guid Id,
    string Name,
    decimal PointsPerUnit,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive);
