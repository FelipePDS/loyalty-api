using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces.Repositories;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Campaigns.GetActive;

public sealed record GetActiveCampaignsQuery : IQuery<IReadOnlyList<CampaignDto>>;

public sealed class GetActiveCampaignsQueryHandler(
    ICampaignRepository campaignRepository)
    : IRequestHandler<GetActiveCampaignsQuery, Result<IReadOnlyList<CampaignDto>>>
{
    public async Task<Result<IReadOnlyList<CampaignDto>>> Handle(
        GetActiveCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var campaigns = await campaignRepository.GetActiveAsync(cancellationToken);
        var dtos = campaigns.Adapt<IReadOnlyList<CampaignDto>>();
        return Result<IReadOnlyList<CampaignDto>>.Success(dtos);
    }
}
