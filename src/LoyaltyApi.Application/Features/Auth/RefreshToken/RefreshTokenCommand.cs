using System.Security.Claims;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Domain.Entities;
using MediatR;

namespace LoyaltyApi.Application.Features.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : ICommand<AuthResponse>;

public sealed class RefreshTokenCommandHandler(
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    ICustomerRepository customerRepository,
    IIdentityService identityService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Validate the expired access token structure
        var principal = tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
            return Error.Unauthorized("Auth.InvalidToken", "Access token is invalid.");

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Error.Unauthorized("Auth.InvalidToken", "Token does not contain user identity.");

        // Hash the incoming refresh token and find it in the DB
        var hashedToken = ComputeHash(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(hashedToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.UserId != userId)
            return Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");

        // Revoke the old refresh token (rotation)
        storedToken.Revoke();

        // Get customer info to rebuild claims
        var customer = await customerRepository.GetByIdentityUserIdAsync(userId, cancellationToken);
        if (customer is null)
            return Error.NotFound("Auth.CustomerNotFound", "Customer profile not found.");

        var role = await identityService.GetRoleByUserIdAsync(userId, cancellationToken) ?? "Customer";

        // Generate new token pair
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AppClaimTypes.CustomerId, customer.Id.ToString()),
            new Claim(ClaimTypes.Email, customer.Email.Value),
            new Claim(ClaimTypes.Role, role)
        };

        var accessToken = tokenService.GenerateAccessToken(claims);
        var newTokenPair = tokenService.GenerateRefreshToken();

        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            userId, newTokenPair.HashedRefreshToken, newTokenPair.RefreshTokenExpiresAt);
        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            newTokenPair.RawRefreshToken,
            newTokenPair.RefreshTokenExpiresAt,
            customer.Id,
            customer.Email.Value,
            role);
    }

    private static string ComputeHash(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
