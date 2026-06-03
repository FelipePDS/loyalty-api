using LoyaltyApi.Domain.Enums;

namespace LoyaltyApi.Application.DTOs;

public sealed record PointTransactionDto(
    Guid Id,
    TransactionType Type,
    int Points,
    string Description,
    string? ReferenceId,
    DateTime? ExpiresAt,
    DateTime CreatedAt);
