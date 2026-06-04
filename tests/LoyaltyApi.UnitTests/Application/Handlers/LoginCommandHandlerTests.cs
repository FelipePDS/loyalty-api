using System.Security.Claims;
using FluentAssertions;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Features.Auth.Login;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.ValueObjects;
using Moq;

namespace LoyaltyApi.UnitTests.Application.Handlers;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _identityServiceMock.Object,
            _customerRepoMock.Object,
            _refreshTokenRepoMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Customer CreateTestCustomer(string identityUserId)
    {
        var email = Email.Create("test@example.com");
        var document = Document.Create("52998224725");
        return Customer.Create("John Doe", email, document, identityUserId);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123");
        var identityUserId = "identity-user-123";
        var userInfo = new IdentityUserInfo(identityUserId, "test@example.com", "Customer");
        var customer = CreateTestCustomer(identityUserId);

        _identityServiceMock
            .Setup(s => s.AuthenticateAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, userInfo, (string?)null));

        _customerRepoMock
            .Setup(r => r.GetByIdentityUserIdAsync(identityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _tokenServiceMock
            .Setup(s => s.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("access-token-123");

        var tokenPair = new TokenPair("access-token-123", "raw-refresh-token", "hashed-refresh-token", DateTime.UtcNow.AddDays(7));
        _tokenServiceMock
            .Setup(s => s.GenerateRefreshToken())
            .Returns(tokenPair);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token-123");
        result.Value.RefreshToken.Should().Be("raw-refresh-token");
        result.Value.CustomerId.Should().Be(customer.Id);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Role.Should().Be("Customer");

        _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ReturnsUnauthorizedError()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword");

        _identityServiceMock
            .Setup(s => s.AuthenticateAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (IdentityUserInfo?)null, "Invalid email or password."));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Code.Should().Be("Auth.InvalidCredentials");

        _customerRepoMock.Verify(r => r.GetByIdentityUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenServiceMock.Verify(s => s.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CustomerProfileNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "Password123");
        var identityUserId = "identity-user-123";
        var userInfo = new IdentityUserInfo(identityUserId, "test@example.com", "Customer");

        _identityServiceMock
            .Setup(s => s.AuthenticateAsync(command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, userInfo, (string?)null));

        _customerRepoMock
            .Setup(r => r.GetByIdentityUserIdAsync(identityUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Auth.CustomerNotFound");
    }
}
