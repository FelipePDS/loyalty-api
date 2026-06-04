using FluentAssertions;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.Enums;
using LoyaltyApi.Domain.Events;
using LoyaltyApi.Domain.Exceptions;
using LoyaltyApi.Domain.ValueObjects;

namespace LoyaltyApi.UnitTests.Domain.Entities;

public sealed class CustomerTests
{
    private static Customer CreateDefaultCustomer()
    {
        var email = Email.Create("test@example.com");
        var document = Document.Create("529.982.247-25"); // Valid CPF
        return Customer.Create("John Doe", email, document, "identity-user-id-123");
    }

    // ─── EarnPoints ──────────────────────────────────────────────────────────

    [Fact]
    public void EarnPoints_ValidPoints_IncreasesBalance()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act
        var transaction = customer.EarnPoints(100, "Purchase reward");

        // Assert
        customer.PointsBalance.Should().Be(100);
        customer.TotalPointsEarned.Should().Be(100);
    }

    [Fact]
    public void EarnPoints_ValidPoints_RaisesPointsEarnedEvent()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act
        customer.EarnPoints(100, "Purchase reward");

        // Assert
        var domainEvents = customer.GetDomainEvents();
        domainEvents.Should().ContainSingle(e => e is PointsEarnedEvent);
        var earnedEvent = (PointsEarnedEvent)domainEvents.First(e => e is PointsEarnedEvent);
        earnedEvent.Points.Should().Be(100);
        earnedEvent.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public void EarnPoints_ZeroOrNegativePoints_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act & Assert
        var act = () => customer.EarnPoints(0, "Invalid");
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void EarnPoints_EmptyDescription_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act & Assert
        var act = () => customer.EarnPoints(100, "");
        act.Should().Throw<DomainException>().WithMessage("*Description*");
    }

    [Fact]
    public void EarnPoints_AccumulatesToSilverTier_RaisesTierUpgradedEvent()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act
        customer.EarnPoints(1000, "Big purchase");

        // Assert
        customer.Tier.Should().Be(CustomerTier.Silver);
        customer.GetDomainEvents().Should().Contain(e => e is CustomerTierUpgradedEvent);
    }

    [Fact]
    public void EarnPoints_AccumulatesToGoldTier_RaisesTierUpgradedEvent()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act
        customer.EarnPoints(5000, "Huge purchase");

        // Assert
        customer.Tier.Should().Be(CustomerTier.Gold);
        var tierEvent = customer.GetDomainEvents().OfType<CustomerTierUpgradedEvent>().Last();
        tierEvent.Should().NotBeNull();
    }

    [Fact]
    public void EarnPoints_MultipleEarns_AccumulatesCorrectly()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act
        customer.EarnPoints(500, "First purchase");
        customer.EarnPoints(600, "Second purchase");

        // Assert
        customer.PointsBalance.Should().Be(1100);
        customer.TotalPointsEarned.Should().Be(1100);
        customer.Tier.Should().Be(CustomerTier.Silver);
    }

    // ─── RedeemPoints ────────────────────────────────────────────────────────

    [Fact]
    public void RedeemPoints_SufficientBalance_DecreasesBalance()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        customer.EarnPoints(500, "Earn first");
        customer.ClearDomainEvents();

        // Act
        var transaction = customer.RedeemPoints(200, "Redeem reward");

        // Assert
        customer.PointsBalance.Should().Be(300);
    }

    [Fact]
    public void RedeemPoints_InsufficientBalance_ThrowsInsufficientPointsException()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        customer.EarnPoints(100, "Earn first");

        // Act & Assert
        var act = () => customer.RedeemPoints(200, "Redeem too much");
        act.Should().Throw<InsufficientPointsException>()
            .Where(e => e.CurrentBalance == 100 && e.RequestedPoints == 200);
    }

    [Fact]
    public void RedeemPoints_ZeroPoints_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        customer.EarnPoints(100, "Earn first");

        // Act & Assert
        var act = () => customer.RedeemPoints(0, "Invalid");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RedeemPoints_DoesNotAffectTotalPointsEarned()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        customer.EarnPoints(500, "Earn first");

        // Act
        customer.RedeemPoints(200, "Redeem some");

        // Assert
        customer.TotalPointsEarned.Should().Be(500);
    }

    // ─── ExpirePoints ────────────────────────────────────────────────────────

    [Fact]
    public void ExpirePoints_MoreThanBalance_FloorsAtZero()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        customer.EarnPoints(50, "Earn first");
        customer.ClearDomainEvents();

        // Act
        var transaction = customer.ExpirePoints(100);

        // Assert
        customer.PointsBalance.Should().Be(0);
    }

    [Fact]
    public void ExpirePoints_LessThanBalance_DeductsExactAmount()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        customer.EarnPoints(200, "Earn first");
        customer.ClearDomainEvents();

        // Act
        customer.ExpirePoints(50);

        // Assert
        customer.PointsBalance.Should().Be(150);
    }

    [Fact]
    public void ExpirePoints_ZeroBalance_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateDefaultCustomer();

        // Act & Assert
        var act = () => customer.ExpirePoints(10);
        act.Should().Throw<DomainException>().WithMessage("*already zero*");
    }

    // ─── ReverseTransaction ──────────────────────────────────────────────────

    [Fact]
    public void ReverseTransaction_EarnedTransaction_ReversesCorrectly()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        var earnedTx = customer.EarnPoints(500, "Original earn");
        customer.ClearDomainEvents();

        // Act
        var reversalTx = customer.ReverseTransaction(earnedTx, "Customer refund");

        // Assert
        customer.PointsBalance.Should().Be(0);
        earnedTx.IsReversed.Should().BeTrue();
        customer.GetDomainEvents().Should().Contain(e => e is PointsReversedEvent);
    }

    [Fact]
    public void ReverseTransaction_InsufficientBalanceForReversal_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateDefaultCustomer();
        var earnedTx = customer.EarnPoints(500, "Original earn");
        customer.RedeemPoints(400, "Spend most");
        customer.ClearDomainEvents();

        // Act & Assert
        var act = () => customer.ReverseTransaction(earnedTx, "Try reverse full amount");
        act.Should().Throw<DomainException>().WithMessage("*negative balance*");
    }

    // ─── Create Factory ──────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidData_ReturnsCustomerWithDefaultTier()
    {
        // Act
        var customer = CreateDefaultCustomer();

        // Assert
        customer.Id.Should().NotBeEmpty();
        customer.FullName.Should().Be("John Doe");
        customer.Tier.Should().Be(CustomerTier.Standard);
        customer.PointsBalance.Should().Be(0);
        customer.TotalPointsEarned.Should().Be(0);
    }

    [Fact]
    public void Create_EmptyFullName_ThrowsDomainException()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var document = Document.Create("529.982.247-25");

        // Act & Assert
        var act = () => Customer.Create("", email, document, "identity-id");
        act.Should().Throw<DomainException>().WithMessage("*Full name*");
    }

    [Fact]
    public void Create_EmptyIdentityUserId_ThrowsDomainException()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var document = Document.Create("529.982.247-25");

        // Act & Assert
        var act = () => Customer.Create("John Doe", email, document, "");
        act.Should().Throw<DomainException>().WithMessage("*Identity user ID*");
    }
}
