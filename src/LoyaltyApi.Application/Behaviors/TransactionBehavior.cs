using LoyaltyApi.Application.Common;
using LoyaltyApi.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoyaltyApi.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            logger.LogDebug("Begin transaction for {RequestName}", requestName);

            var response = await next();
            await unitOfWork.CommitAsync(cancellationToken);

            logger.LogDebug("Committed transaction for {RequestName}", requestName);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            logger.LogWarning("Rolled back transaction for {RequestName}", requestName);
            throw;
        }
    }
}
