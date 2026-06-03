using LoyaltyApi.Application.Interfaces.Services;
using LoyaltyApi.Domain.Entities;
using LoyaltyApi.Domain.Enums;
using LoyaltyApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoyaltyApi.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    private readonly IEncryptionService _encryption;

    public CustomerConfiguration(IEncryptionService encryption) => _encryption = encryption;

    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(200);

        // Email value object — encrypt at rest
        var emailConverter = new ValueConverter<Email, string>(
            vo => _encryption.Encrypt(vo.Value),
            raw => Email.Create(_encryption.Decrypt(raw)));

        builder.Property(c => c.Email)
            .HasConversion(emailConverter)
            .IsRequired()
            .HasMaxLength(512) // encrypted value is longer than plaintext
            .HasColumnName("Email");

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Email");

        // Document value object — encrypt at rest
        var documentConverter = new ValueConverter<Document, string>(
            vo => _encryption.Encrypt(vo.Value),
            raw => Document.Create(_encryption.Decrypt(raw)));

        builder.Property(c => c.Document)
            .HasConversion(documentConverter)
            .IsRequired()
            .HasMaxLength(512)
            .HasColumnName("Document");

        builder.Property(c => c.Tier)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.PointsBalance)
            .IsRequired();

        builder.Property(c => c.TotalPointsEarned)
            .IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // Navigation — a customer owns many transactions (loaded explicitly / no cascade in queries)
        builder.HasMany<Domain.Entities.PointTransaction>()
            .WithOne()
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
