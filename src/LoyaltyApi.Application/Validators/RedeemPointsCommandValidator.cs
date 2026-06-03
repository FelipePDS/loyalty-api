using FluentValidation;
using LoyaltyApi.Application.Features.Points.Redeem;

namespace LoyaltyApi.Application.Validators;

public sealed class RedeemPointsCommandValidator : AbstractValidator<RedeemPointsCommand>
{
    public RedeemPointsCommandValidator()
    {
        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than zero.");
    }
}
