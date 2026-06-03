using FluentValidation;
using LoyaltyApi.Application.Features.Points.Earn;

namespace LoyaltyApi.Application.Validators;

public sealed class EarnPointsCommandValidator : AbstractValidator<EarnPointsCommand>
{
    public EarnPointsCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than zero.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
