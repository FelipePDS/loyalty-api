using FluentValidation.Results;

namespace LoyaltyApi.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    Internal
}

public record Error(string Code, string Message, ErrorType Type)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Internal(string code, string message) => new(code, message, ErrorType.Internal);
}

public sealed record ValidationError : Error
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private ValidationError(IReadOnlyDictionary<string, string[]> errors)
        : base("Validation", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = errors;
    }

    public static ValidationError Create(IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return new ValidationError(errors);
    }
}
