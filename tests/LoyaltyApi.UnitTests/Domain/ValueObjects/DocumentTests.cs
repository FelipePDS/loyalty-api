using FluentAssertions;
using LoyaltyApi.Domain.ValueObjects;

namespace LoyaltyApi.UnitTests.Domain.ValueObjects;

public sealed class DocumentTests
{
    [Fact]
    public void Create_ValidCpfWithMask_StripsNonDigits()
    {
        // Act
        var doc = Document.Create("529.982.247-25");

        // Assert
        doc.Value.Should().Be("52998224725");
    }

    [Fact]
    public void Create_DigitsOnly_ReturnsDocument()
    {
        // Act
        var doc = Document.Create("52998224725");

        // Assert
        doc.Value.Should().Be("52998224725");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrEmpty_ThrowsArgumentException(string? value)
    {
        // Act & Assert
        var act = () => Document.Create(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NoDigits_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => Document.Create("abc-def");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsValid_ValidCpf_ReturnsTrue()
    {
        // Arrange — valid CPFs
        var doc = Document.Create("529.982.247-25");

        // Act & Assert
        doc.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("11144477735")] // Valid CPF: 111.444.777-35
    [InlineData("52998224725")] // Valid CPF: 529.982.247-25
    public void IsValid_KnownValidCpfs_ReturnsTrue(string cpf)
    {
        // Arrange
        var doc = Document.Create(cpf);

        // Act & Assert
        doc.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void IsValid_AllSameDigits_ReturnsFalse(string cpf)
    {
        // Arrange
        var doc = Document.Create(cpf);

        // Act & Assert
        doc.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("52998224720")] // wrong check digit
    public void IsValid_InvalidCheckDigits_ReturnsFalse(string cpf)
    {
        // Arrange
        var doc = Document.Create(cpf);

        // Act & Assert
        doc.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WrongLength_ReturnsFalse()
    {
        // Arrange
        var doc = Document.Create("1234567");

        // Act & Assert
        doc.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var doc1 = Document.Create("529.982.247-25");
        var doc2 = Document.Create("52998224725");

        // Assert
        doc1.Should().Be(doc2);
    }
}
