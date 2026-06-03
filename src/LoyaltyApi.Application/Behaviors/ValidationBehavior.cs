using FluentValidation;
using LoyaltyApi.Application.Common;
using MediatR;

namespace LoyaltyApi.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var validationError = ValidationError.Create(failures);

        // Build the appropriate Result failure based on TResponse type.
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(validationError);
        }

        // Result<T> case — use reflection to call Result<T>.Failure(error).
        var resultType = typeof(TResponse);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = resultType.GetMethod(nameof(Result<object>.Failure), [typeof(Error)])!;
            return (TResponse)failureMethod.Invoke(null, [validationError])!;
        }

        return await next();
    }
}
