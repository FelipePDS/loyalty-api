using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Entities;
using Mapster;
using MediatR;

namespace LoyaltyApi.Application.Features.Campaigns.Create;

public sealed record CreateCampaignCommand(
    string Name,
    decimal PointsPerUnit,
    DateTime StartDate,
    DateTime EndDate) : ICommand<CampaignDto>;

public sealed class CreateCampaignCommandHandler(
    ICampaignRepository campaignRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCampaignCommand, Result<CampaignDto>>
{
    public async Task<Result<CampaignDto>> Handle(
        CreateCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = Campaign.Create(
            request.Name,
            request.PointsPerUnit,
            request.StartDate,
            request.EndDate);

        await campaignRepository.AddAsync(campaign, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return campaign.Adapt<CampaignDto>();
    }
}
