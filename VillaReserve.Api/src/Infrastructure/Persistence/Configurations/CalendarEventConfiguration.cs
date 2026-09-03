using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VillaReserve.Api.Domain.Entities;
using VillaReserve.Api.Domain.Enums;

namespace VillaReserve.Api.Infrastructure.Persistence.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.ReservationId)
            .HasColumnName("reservation_id")
            .IsRequired();

        var providerConverter = new ValueConverter<CalendarProvider, string>(
            v => v.ToString().ToUpperInvariant(),
            v => Enum.Parse<CalendarProvider>(v, true));

        builder.Property(c => c.Provider)
            .HasColumnName("provider")
            .HasConversion(providerConverter)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ExternalEventId)
            .HasColumnName("external_event_id")
            .HasMaxLength(256)
            .IsRequired();

        var syncStatusConverter = new ValueConverter<CalendarSyncStatus, string>(
            v => v.ToString().ToUpperInvariant(),
            v => Enum.Parse<CalendarSyncStatus>(v, true));

        builder.Property(c => c.SyncStatus)
            .HasColumnName("sync_status")
            .HasConversion(syncStatusConverter)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(c => c.ReservationId)
            .IsUnique();

        builder.HasIndex(c => new { c.Provider, c.ExternalEventId })
            .IsUnique();

        builder.HasOne(c => c.Reservation)
            .WithOne(r => r.CalendarEvent)
            .HasForeignKey<CalendarEvent>(c => c.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
