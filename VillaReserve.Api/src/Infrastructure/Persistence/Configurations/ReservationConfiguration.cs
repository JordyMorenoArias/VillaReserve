using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VillaReserve.Api.Domain.Entities;
using VillaReserve.Api.Domain.Enums;

namespace VillaReserve.Api.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations", t =>
        {
            t.HasCheckConstraint("ck_reservations_end_after_start", "end_datetime > start_datetime");
            t.HasCheckConstraint("ck_reservations_guest_count", "guest_count IS NULL OR guest_count > 0");
            t.HasCheckConstraint("ck_reservations_confirmed_at", "status != 'CONFIRMED' OR confirmed_at IS NOT NULL");
            t.HasCheckConstraint("ck_reservations_cancelled_at", "status != 'CANCELLED' OR cancelled_at IS NOT NULL");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.CustomerPhone)
            .HasColumnName("customer_phone")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.CustomerEmail)
            .HasColumnName("customer_email")
            .HasMaxLength(256);

        builder.Property(r => r.GuestCount)
            .HasColumnName("guest_count");

        builder.Property(r => r.StartDateTime)
            .HasColumnName("start_datetime")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.EndDateTime)
            .HasColumnName("end_datetime")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        var statusConverter = new ValueConverter<ReservationStatus, string>(
            v => v.ToString().ToUpperInvariant(),
            v => Enum.Parse<ReservationStatus>(v, true));

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion(statusConverter)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Notes)
            .HasColumnName("notes");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(r => r.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasOne(r => r.CalendarEvent)
            .WithOne(c => c.Reservation)
            .HasForeignKey<CalendarEvent>(c => c.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.ReservationTokens)
            .WithOne(t => t.Reservation)
            .HasForeignKey(t => t.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
