using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoyaltyApi.Infrastructure.BackgroundServices;

/// <summary>
/// Runs once daily at 02:00 UTC. Processes expired point transactions in batches of 500.
///
/// Algorithm:
///   1. Query all Earned transactions whose ExpiresAt &lt;= UtcNow.
///   2. Group by CustomerId.
///   3. For each customer, call Customer.ExpirePoints(totalExpired) to deduct from balance.
///   4. Save via UoW (which writes domain events to outbox).
///   5. Log summary.
///
/// Uses a dedicated DI scope per run so DbContext is not shared with the web request pipeline.
/// </summary>
public sealed class PointExpirationBackgroundService : BackgroundService
{
    private const int BatchSize = 500;
    private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PointExpirationBackgroundService> _logger;

    public PointExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PointExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PointExpirationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            _logger.LogInformation(
                "Next expiration run scheduled in {DelayMinutes} minutes (at 02:00 UTC).",
                delay.TotalMinutes);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunExpirationBatchAsync(stoppingToken);
        }

        _logger.LogInformation("PointExpirationBackgroundService stopped.");
    }

    private async Task RunExpirationBatchAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting point expiration run at {UtcNow:O}", DateTime.UtcNow);

        int totalProcessed = 0;
        int totalFailed = 0;
        var cutoff = DateTime.UtcNow;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var transactionRepo = scope.ServiceProvider.GetRequiredService<IPointTransactionRepository>();
            var customerRepo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var expiredTransactions = await transactionRepo
                .GetExpiredUnprocessedAsync(cutoff, cancellationToken);

            _logger.LogInformation(
                "Found {Count} expired transaction records to process.", expiredTransactions.Count);

            // Group by customer so we make one domain call per customer.
            var byCustomer = expiredTransactions
                .GroupBy(t => t.CustomerId)
                .ToList();

            // Process in batches of BatchSize customers.
            foreach (var customerBatch in byCustomer.Chunk(BatchSize))
            {
                foreach (var group in customerBatch)
                {
                    try
                    {
                        // Load the tracked customer entity for the write operation.
                        var customer = await customerRepo.GetByIdAsync(group.Key, cancellationToken);
                        if (customer is null)
                        {
                            _logger.LogWarning(
                                "Customer {CustomerId} not found during expiration run — skipping.", group.Key);
                            continue;
                        }

                        var totalToExpire = group.Sum(t => t.Points);
                        if (totalToExpire <= 0 || customer.PointsBalance == 0)
                            continue;

                        // Re-attach for tracking (GetByIdAsync uses AsNoTracking).
                        customerRepo.Update(customer);

                        var expirationTransaction = customer.ExpirePoints(totalToExpire);
                        await transactionRepo.AddAsync(expirationTransaction, cancellationToken);

                        totalProcessed++;
                    }
                    catch (DomainException ex)
                    {
                        // Log and continue — one failing customer must not abort the whole batch.
                        _logger.LogWarning(ex,
                            "Domain error expiring points for customer {CustomerId}.", group.Key);
                        totalFailed++;
                    }
                }

                await uow.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Point expiration run was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during point expiration run.");
        }

        _logger.LogInformation(
            "Point expiration run complete. Processed: {Processed}, Failed: {Failed}.",
            totalProcessed, totalFailed);
    }

    /// <summary>Calculates the delay until the next 02:00 UTC run.</summary>
    private static TimeSpan CalculateDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2); // 02:00 UTC today

        if (now >= nextRun)
            nextRun = nextRun.AddDays(1); // Already past 02:00 — schedule for tomorrow

        return nextRun - now;
    }
}
