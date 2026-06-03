using System.Security.Claims;
using LoyaltyApi.API.Extensions;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.Features.Customers.AdjustPoints;
using LoyaltyApi.Application.Features.Customers.GetAll;
using LoyaltyApi.Application.Features.Customers.GetBalance;
using LoyaltyApi.Application.Features.Customers.GetProfile;
using LoyaltyApi.Application.Features.Customers.GetTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LoyaltyApi.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
[EnableRateLimiting("api")]
public sealed class CustomersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IResult> GetProfile(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromClaims();
        var result = await sender.Send(new GetCustomerProfileQuery(customerId), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("me/balance")]
    public async Task<IResult> GetBalance(CancellationToken cancellationToken)
    {
        var customerId = GetCustomerIdFromClaims();
        var result = await sender.Send(new GetCustomerBalanceQuery(customerId), cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet("me/transactions")]
    public async Task<IResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerIdFromClaims();
        var result = await sender.Send(
            new GetTransactionHistoryQuery(customerId, page, pageSize),
            cancellationToken);
        return result.ToHttpResult();
    }

    [HttpGet]
    [Authorize("AdminOnly")]
    public async Task<IResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAllCustomersQuery(page, pageSize),
            cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("{customerId:guid}/points/adjust")]
    [Authorize("AdminOnly")]
    public async Task<IResult> AdjustPoints(
        Guid customerId,
        [FromBody] AdjustPointsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AdjustPointsCommand(customerId, request.Points, request.Description);
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

public sealed record AdjustPointsRequest(int Points, string Description);
