using LoyaltyApi.Application.Interfaces;
using LoyaltyApi.Application.Interfaces.Repositories;
using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Infrastructure.BackgroundServices;
using LoyaltyApi.Infrastructure.Persistence;
using LoyaltyApi.Infrastructure.Repositories;
using LoyaltyApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoyaltyApi.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Infrastructure-layer services:
    /// DbContext, repositories, unit of work, encryption, token service, and background service.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EncryptionService must be registered before DbContext because the DbContext
        // constructor receives it directly for use in EF value converters.
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // EF Core DbContext with SQL Server + Polly retry via EnableRetryOnFailure
        services.AddDbContext<LoyaltyApiDbContext>((sp, options) =>
        {
            var encryption = sp.GetRequiredService<IEncryptionService>();
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql =>
                {
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sql.MigrationsAssembly(typeof(LoyaltyApiDbContext).Assembly.FullName);
                });
        });

        // Repositories — scoped to match DbContext lifetime
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Token service — singleton because it is stateless and parsing JWTs is thread-safe
        services.AddSingleton<ITokenService, TokenService>();

        // Background service for daily point expiration
        services.AddHostedService<PointExpirationBackgroundService>();

        return services;
    }
}
