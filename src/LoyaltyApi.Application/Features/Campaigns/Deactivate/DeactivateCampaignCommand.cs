using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using MediatR;

namespace LoyaltyApi.Application.Features.Campaigns.Deactivate;

public sealed record DeactivateCampaignCommand(Guid CampaignId) : ICommand;

public sealed class DeactivateCampaignCommandHandler(
    ICampaignRepository campaignRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateCampaignCommand, Result>
{
    public async Task<Result> Handle(
        DeactivateCampaignCommand request,
        CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.CampaignId, cancellationToken);
        if (campaign is null)
            return Error.NotFound("Campaign.NotFound", $"Campaign {request.CampaignId} not found.");

        campaign.Deactivate();
        campaignRepository.Update(campaign);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
