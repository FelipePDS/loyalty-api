using FluentValidation;
using LoyaltyApi.Application.Features.Auth.Register;
using LoyaltyApi.Domain.ValueObjects;

namespace LoyaltyApi.Application.Validators;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Document (CPF) is required.")
            .Must(BeValidCpf).WithMessage("Document must be a valid CPF.");
    }

    private static bool BeValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        try
        {
            var document = Document.Create(cpf);
            return document.IsValid();
        }
        catch
        {
            return false;
        }
    }
}
