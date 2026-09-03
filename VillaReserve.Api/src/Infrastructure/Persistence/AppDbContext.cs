using Microsoft.EntityFrameworkCore;
using VillaReserve.Api.Domain.Entities;

namespace VillaReserve.Api.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core database context.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<BlockedPeriod> BlockedPeriods => Set<BlockedPeriod>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ReservationToken> ReservationTokens => Set<ReservationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgcrypto for PostgreSQL-generated UUIDs.
        modelBuilder.HasPostgresExtension("pgcrypto");

        // Enable btree_gist, required for exclusion constraints
        // that prevent overlapping reservation intervals.
        modelBuilder.HasPostgresExtension("btree_gist");

        // Apply all entity configurations from this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
