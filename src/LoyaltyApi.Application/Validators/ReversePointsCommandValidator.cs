using FluentValidation;
using LoyaltyApi.Application.Features.Points.Reverse;

namespace LoyaltyApi.Application.Validators;

public sealed class ReversePointsCommandValidator : AbstractValidator<ReversePointsCommand>
{
    public ReversePointsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty().WithMessage("Transaction ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reversal reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
