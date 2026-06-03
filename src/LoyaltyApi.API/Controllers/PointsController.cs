using System.Security.Claims;
using LoyaltyApi.API.Extensions;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.Features.Points.Earn;
using LoyaltyApi.Application.Features.Points.Redeem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LoyaltyApi.API.Controllers;

[ApiController]
[Route("api/points")]
[Authorize]
[EnableRateLimiting("api")]
public sealed class PointsController(ISender sender) : ControllerBase
{
    [HttpPost("earn")]
    [Authorize("PartnerOrAdmin")]
    public async Task<IResult> Earn(
        [FromBody] EarnPointsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedHttpResult<Application.DTOs.PointTransactionDto>();
    }

    [HttpPost("redeem")]
    public async Task<IResult> Redeem(
        [FromBody] RedeemPointsRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromClaims();
        var command = new RedeemPointsCommand(customerId, request.Points, request.Description);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private Guid GetCustomerIdFromClaims()
    {
        var claim = User.FindFirstValue(AppClaimTypes.CustomerId)
            ?? throw new UnauthorizedAccessException("CustomerId claim not found.");
        return Guid.Parse(claim);
    }
}

public sealed record RedeemPointsRequest(int Points, string Description);
