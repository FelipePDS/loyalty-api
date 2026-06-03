using System.Security.Claims;
using LoyaltyApi.API.Extensions;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.Features.Auth.Login;
using LoyaltyApi.Application.Features.Auth.Logout;
using LoyaltyApi.Application.Features.Auth.RefreshToken;
using LoyaltyApi.Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LoyaltyApi.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IResult> Register(
        [FromBody] RegisterCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToCreatedHttpResult<Application.DTOs.AuthResponse>();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new LogoutCommand(userId, request.RefreshToken);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }
}

public sealed record LogoutRequest(string RefreshToken);
