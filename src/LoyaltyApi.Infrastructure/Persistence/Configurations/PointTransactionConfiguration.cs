using LoyaltyApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoyaltyApi.Infrastructure.Persistence.Configurations;

internal sealed class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.ToTable("PointTransactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.CustomerId)
            .IsRequired();

        builder.Property(t => t.Points)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.ReferenceId)
            .HasMaxLength(200);

        builder.Property(t => t.ExpiresAt);

        builder.Property(t => t.CreatedAt).IsRequired();

        // Composite index for expiration queries
        builder.HasIndex(t => new { t.CustomerId, t.ExpiresAt })
            .HasFilter("\"ExpiresAt\" IS NOT NULL")
            .HasDatabaseName("IX_PointTransactions_CustomerId_ExpiresAt");

        // Covering index for transaction history queries
        builder.HasIndex(t => t.CustomerId)
            .HasDatabaseName("IX_PointTransactions_CustomerId");
    }
}
