using LoyaltyApi.API.Extensions;
using LoyaltyApi.Application.Features.Campaigns.Create;
using LoyaltyApi.Application.Features.Campaigns.Deactivate;
using LoyaltyApi.Application.Features.Campaigns.GetActive;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LoyaltyApi.API.Controllers;

[ApiController]
[Route("api/campaigns")]
[EnableRateLimiting("api")]
public sealed class CampaignsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetActiveCampaignsQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost]
    [Authorize("AdminOnly")]
    public async Task<IResult> Create(
        [FromBody] CreateCampaignCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedHttpResult<Application.DTOs.CampaignDto>();
    }

    [HttpDelete("{id:guid}")]
    [Authorize("AdminOnly")]
    public async Task<IResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateCampaignCommand(id), cancellationToken);
        return result.ToHttpResult();
    }
}
