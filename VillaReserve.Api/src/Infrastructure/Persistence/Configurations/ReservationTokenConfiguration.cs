using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VillaReserve.Api.Domain.Entities;
using VillaReserve.Api.Domain.Enums;

namespace VillaReserve.Api.Infrastructure.Persistence.Configurations;

public sealed class ReservationTokenConfiguration : IEntityTypeConfiguration<ReservationToken>
{
    public void Configure(EntityTypeBuilder<ReservationToken> builder)
    {
        builder.ToTable("reservation_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.ReservationId)
            .HasColumnName("reservation_id")
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(256)
            .IsRequired();

        var purposeConverter = new ValueConverter<ReservationTokenPurpose, string>(
            v => v == ReservationTokenPurpose.AdminAccess ? "ADMIN_ACCESS" : "CUSTOMER_ACCESS",
            v => v == "ADMIN_ACCESS" ? ReservationTokenPurpose.AdminAccess : ReservationTokenPurpose.CustomerAccess);

        builder.Property(t => t.Purpose)
            .HasColumnName("purpose")
            .HasConversion(purposeConverter)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.HasIndex(t => t.ReservationId);

        builder.HasOne(t => t.Reservation)
            .WithMany(r => r.ReservationTokens)
            .HasForeignKey(t => t.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
