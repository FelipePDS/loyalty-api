using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LoyaltyApi.Application.Behaviors;
using LoyaltyApi.Application.Common;
using MediatR;
using Moq;

namespace LoyaltyApi.UnitTests.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    // ─── Test command types ──────────────────────────────────────────────────

    public sealed record TestCommand(string Name, int Value) : ICommand<string>;

    private static RequestHandlerDelegate<Result<string>> CreateNext(Result<string> returnValue)
    {
        return (ct) => Task.FromResult(returnValue);
    }

    // ─── No validators → passes through ─────────────────────────────────────

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("valid", 10);
        var next = CreateNext(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    // ─── Valid command → passes through ──────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_CallsNext()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("valid", 10);
        var next = CreateNext(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    // ─── Invalid command → returns failure with validation errors ─────────────

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsFailureWithValidationErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required."),
            new("Value", "Value must be positive.")
        };

        var validatorMock = new Mock<IValidator<TestCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { validatorMock.Object };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("", -1);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result<string>.Success("should not reach here"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();

        var validationError = (ValidationError)result.Error!;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors.Should().ContainKey("Value");
        validationError.Errors["Name"].Should().Contain("Name is required.");
        validationError.Errors["Value"].Should().Contain("Value must be positive.");

        nextCalled.Should().BeFalse();
    }

    // ─── Multiple validators — aggregates all failures ───────────────────────

    [Fact]
    public async Task Handle_MultipleValidatorsWithFailures_AggregatesAllErrors()
    {
        // Arrange
        var validator1Mock = new Mock<IValidator<TestCommand>>();
        validator1Mock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Too short.") }));

        var validator2Mock = new Mock<IValidator<TestCommand>>();
        validator2Mock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Value", "Must be positive.") }));

        var validators = new[] { validator1Mock.Object, validator2Mock.Object };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("x", -5);
        var next = CreateNext(Result<string>.Success("should not be called"));

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        var validationError = (ValidationError)result.Error!;
        validationError.Errors.Should().HaveCount(2);
    }
}
