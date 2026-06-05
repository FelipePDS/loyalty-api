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

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            logger.LogDebug("Begin transaction for {RequestName}", requestName);

            var response = await next();

            logger.LogDebug("Committed transaction for {RequestName}", requestName);
            return response;
        }, cancellationToken);
    }
}
