namespace LoyaltyApi.Domain.Exceptions;

/// <summary>
/// Thrown exclusively for domain rule violations. Must never be used for
/// infrastructure failures, argument validation, or programming errors.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
