using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoyaltyApi.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var isSuccess = response switch
        {
            Common.Result result => result.IsSuccess,
            _ => true
        };

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms — Success: {IsSuccess}",
            requestName,
            stopwatch.ElapsedMilliseconds,
            isSuccess);

        return response;
    }
}
