using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.Enums;
using Mapster;

namespace LoyaltyApi.Application.Mapping;

public static class MappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<PointTransaction, PointTransactionDto>.NewConfig()
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        TypeAdapterConfig<Campaign, CampaignDto>.NewConfig();

        TypeAdapterConfig<Customer, CustomerProfileDto>.NewConfig()
            .Map(dest => dest.CustomerId, src => src.Id)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.PointsToNextTier, src => CalculatePointsToNextTier(src))
            .Map(dest => dest.NextTier, src => GetNextTier(src.Tier));

        TypeAdapterConfig<Customer, CustomerSummaryDto>.NewConfig()
            .Map(dest => dest.Email, src => src.Email.Value);

        TypeAdapterConfig<Customer, CustomerBalanceDto>.NewConfig()
            .Map(dest => dest.CustomerId, src => src.Id);
    }

    private static int CalculatePointsToNextTier(Customer customer)
    {
        return customer.Tier switch
        {
            CustomerTier.Standard => 1000 - customer.TotalPointsEarned,
            CustomerTier.Silver => 5000 - customer.TotalPointsEarned,
            CustomerTier.Gold => 0,
            _ => 0
        };
    }

    private static CustomerTier? GetNextTier(CustomerTier current)
    {
        return current switch
        {
            CustomerTier.Standard => CustomerTier.Silver,
            CustomerTier.Silver => CustomerTier.Gold,
            _ => null
        };
    }
}
