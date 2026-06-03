using System.Security.Claims;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.ValueObjects;
using MediatR;

namespace LoyaltyApi.Application.Features.Auth.Register;

public sealed record RegisterCustomerCommand(
    string FullName,
    string Email,
    string Password,
    string Document) : ICommand<AuthResponse>;

public sealed class RegisterCustomerCommandHandler(
    IIdentityService identityService,
    ICustomerRepository customerRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCustomerCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        RegisterCustomerCommand request,
        CancellationToken cancellationToken)
    {
        // Create Identity user
        var (succeeded, userId, error) = await identityService.CreateUserAsync(
            request.Email, request.Password, "Customer", cancellationToken);

        if (!succeeded)
            return Error.Conflict("Auth.UserExists", error ?? "Failed to create user.");

        // Create domain entity
        var email = Domain.ValueObjects.Email.Create(request.Email);
        var document = Domain.ValueObjects.Document.Create(request.Document);
        var customer = Customer.Create(request.FullName, email, document, userId!);

        await customerRepository.AddAsync(customer, cancellationToken);

        // Generate tokens
        var claims = BuildClaims(userId!, customer.Id, request.Email, "Customer");
        var accessToken = tokenService.GenerateAccessToken(claims);
        var tokenPair = tokenService.GenerateRefreshToken();

        var refreshToken = Domain.Entities.RefreshToken.Create(userId!, tokenPair.HashedRefreshToken, tokenPair.RefreshTokenExpiresAt);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            tokenPair.RawRefreshToken,
            tokenPair.RefreshTokenExpiresAt,
            customer.Id,
            request.Email,
            "Customer");
    }

    private static List<Claim> BuildClaims(string userId, Guid customerId, string email, string role)
    {
        return
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AppClaimTypes.CustomerId, customerId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        ];
    }
}
