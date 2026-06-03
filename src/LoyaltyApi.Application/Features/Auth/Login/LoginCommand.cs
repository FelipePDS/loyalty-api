using System.Security.Claims;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Domain.Entities;
using MediatR;

namespace LoyaltyApi.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    ICustomerRepository customerRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var (succeeded, userInfo, error) = await identityService.AuthenticateAsync(
            request.Email, request.Password, cancellationToken);

        if (!succeeded || userInfo is null)
            return Error.Unauthorized("Auth.InvalidCredentials", error ?? "Invalid email or password.");

        var customer = await customerRepository.GetByIdentityUserIdAsync(userInfo.UserId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Auth.CustomerNotFound", "Customer profile not found.");

        // Generate tokens
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(AppClaimTypes.CustomerId, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, userInfo.Email),
            new Claim(ClaimTypes.Role, userInfo.Role)
        };

        var accessToken = tokenService.GenerateAccessToken(claims);
        var tokenPair = tokenService.GenerateRefreshToken();

        var refreshToken = Domain.Entities.RefreshToken.Create(userInfo.UserId, tokenPair.HashedRefreshToken, tokenPair.RefreshTokenExpiresAt);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            tokenPair.RawRefreshToken,
            tokenPair.RefreshTokenExpiresAt,
            customer.Id,
            userInfo.Email,
            userInfo.Role);
    }
}
