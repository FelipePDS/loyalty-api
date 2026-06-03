using LoyaltyApi.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoyaltyApi.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(o => o.OccurredAt).IsRequired();
        builder.Property(o => o.ProcessedAt);

        // Allow the outbox processor to efficiently query unprocessed messages
        builder.HasIndex(o => o.ProcessedAt)
            .HasFilter("[ProcessedAt] IS NULL")
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt_Unprocessed");
    }
}
