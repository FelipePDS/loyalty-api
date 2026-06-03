using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Domain.Common;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Infrastructure.Persistence.Configurations;
using LoyaltyApi.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LoyaltyApi.Infrastructure.Persistence;

/// <summary>
/// Central DbContext. Inherits from <see cref="IdentityDbContext"/> so ASP.NET Core Identity
/// tables live in the same database as the domain tables.
///
/// <see cref="IEncryptionService"/> is injected so EF value converters can encrypt/decrypt
/// Customer.Email and Customer.Document at the persistence boundary.
/// </summary>
public sealed class LoyaltyApiDbContext : IdentityDbContext<IdentityUser>
{
    private readonly IEncryptionService _encryption;

    public LoyaltyApiDbContext(
        DbContextOptions<LoyaltyApiDbContext> options,
        IEncryptionService encryption)
        : base(options)
    {
        _encryption = encryption;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Must be called first for Identity tables.

        builder.ApplyConfiguration(new CustomerConfiguration(_encryption));
        builder.ApplyConfiguration(new PointTransactionConfiguration());
        builder.ApplyConfiguration(new CampaignConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new OutboxMessageConfiguration());

        // Global soft-delete filter — applied to every entity that implements ISoftDeletable.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var condition = System.Linq.Expressions.Expression.Equal(
                    property,
                    System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
