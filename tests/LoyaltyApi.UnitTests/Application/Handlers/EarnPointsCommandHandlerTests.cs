using FluentAssertions;
using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.DTOs;
using LoyaltyApi.Application.Features.Points.Earn;
using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.ValueObjects;
using Moq;

namespace LoyaltyApi.UnitTests.Application.Handlers;

public sealed class EarnPointsCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IPointTransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly EarnPointsCommandHandler _handler;

    public EarnPointsCommandHandlerTests()
    {
        _handler = new EarnPointsCommandHandler(
            _customerRepoMock.Object,
            _transactionRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Customer CreateTestCustomer()
    {
        var email = Email.Create("test@example.com");
        var document = Document.Create("52998224725");
        return Customer.Create("John Doe", email, document, "identity-123");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithDto()
    {
        // Arrange
        var customer = CreateTestCustomer();
        var command = new EarnPointsCommand(customer.Id, 100, "Purchase reward", null, null);

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
        result.Value.Points.Should().Be(100);
        result.Value.Type.Should().Be(global::LoyaltyApi.Domain.Enums.TransactionType.Earned);

        _customerRepoMock.Verify(r => r.Update(customer), Times.Once);
        _transactionRepoMock.Verify(r => r.AddAsync(It.IsAny<PointTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new EarnPointsCommand(customerId, 100, "Purchase reward", null, null);

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Customer.NotFound");

        _customerRepoMock.Verify(r => r.Update(It.IsAny<Customer>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithReferenceIdAndExpiry_PersistsTransaction()
    {
        // Arrange
        var customer = CreateTestCustomer();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var command = new EarnPointsCommand(customer.Id, 50, "Campaign bonus", "REF-001", expiresAt);

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
        result.Value.Points.Should().Be(50);
        result.Value.ReferenceId.Should().Be("REF-001");
    }
}
