using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VillaReserve.Api.Domain.Entities;

namespace VillaReserve.Api.Infrastructure.Persistence.Configurations;

public sealed class BlockedPeriodConfiguration : IEntityTypeConfiguration<BlockedPeriod>
{
    public void Configure(EntityTypeBuilder<BlockedPeriod> builder)
    {
        builder.ToTable("blocked_periods", t =>
        {
            t.HasCheckConstraint("ck_blocked_periods_end_after_start", "end_datetime > start_datetime");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasColumnName("id");

        builder.Property(b => b.StartDateTime)
            .HasColumnName("start_datetime")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(b => b.EndDateTime)
            .HasColumnName("end_datetime")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(b => b.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.HasIndex(b => b.CreatedBy);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(b => b.RowVersion)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasOne(b => b.CreatedByUser)
            .WithMany(u => u.BlockedPeriods)
            .HasForeignKey(b => b.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
