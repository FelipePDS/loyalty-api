using FluentAssertions;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Features.Points.Redeem;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.ValueObjects;
using Moq;

namespace LoyaltyApi.UnitTests.Application.Handlers;

public sealed class RedeemPointsCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IPointTransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RedeemPointsCommandHandler _handler;

    public RedeemPointsCommandHandlerTests()
    {
        _handler = new RedeemPointsCommandHandler(
            _customerRepoMock.Object,
            _transactionRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Customer CreateTestCustomerWithBalance(int balance)
    {
        var email = Email.Create("test@example.com");
        var document = Document.Create("52998224725");
        var customer = Customer.Create("John Doe", email, document, "identity-123");
        if (balance > 0)
            customer.EarnPoints(balance, "Initial balance");
        customer.ClearDomainEvents();
        return customer;
    }

    [Fact]
    public async Task Handle_SufficientBalance_ReturnsSuccessWithDto()
    {
        // Arrange
        var customer = CreateTestCustomerWithBalance(500);
        var command = new RedeemPointsCommand(customer.Id, 200, "Reward redemption");

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Points.Should().Be(-200);
        result.Value.Type.Should().Be(global::LoyaltyApi.Domain.Enums.TransactionType.Redeemed);

        _customerRepoMock.Verify(r => r.Update(customer), Times.Once);
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<PointTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientBalance_ReturnsValidationError()
    {
        // Arrange
        var customer = CreateTestCustomerWithBalance(100);
        var command = new RedeemPointsCommand(customer.Id, 200, "Redeem too much");

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Points.InsufficientBalance");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new RedeemPointsCommand(customerId, 100, "Redeem");

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Customer.NotFound");
    }
}
