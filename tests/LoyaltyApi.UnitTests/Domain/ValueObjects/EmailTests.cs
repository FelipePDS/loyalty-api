using FluentAssertions;
using LoyaltyApi.Domain.ValueObjects;

namespace LoyaltyApi.UnitTests.Domain.ValueObjects;

public sealed class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@domain.co")]
    [InlineData("USER@EXAMPLE.COM")]
    public void Create_ValidEmail_ReturnsEmailObject(string value)
    {
        // Act
        var email = Email.Create(value);

        // Assert
        email.Value.Should().Be(value.ToLowerInvariant().Trim());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrEmpty_ThrowsArgumentException(string? value)
    {
        // Act & Assert
        var act = () => Email.Create(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    [InlineData("no-tld@domain")]
    [InlineData("spaces in@email.com")]
    public void Create_InvalidFormat_ThrowsArgumentException(string value)
    {
        // Act & Assert
        var act = () => Email.Create(value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NormalizesToLowerCase()
    {
        // Act
        var email = Email.Create("John.Doe@EXAMPLE.COM");

        // Assert
        email.Value.Should().Be("john.doe@example.com");
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("TEST@EXAMPLE.COM");

        // Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("user1@example.com");
        var email2 = Email.Create("user2@example.com");

        // Assert
        email1.Should().NotBe(email2);
    }
}
