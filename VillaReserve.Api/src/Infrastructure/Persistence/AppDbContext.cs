using Microsoft.EntityFrameworkCore;

namespace VillaReserve.Api.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core database context.
/// Entity configurations will be added here as features are implemented.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgcrypto for PostgreSQL-generated UUIDs.
        modelBuilder.HasPostgresExtension("pgcrypto");

        // Enable btree_gist, required later for exclusion constraints
        // that prevent overlapping reservation intervals.
        modelBuilder.HasPostgresExtension("btree_gist");

        // Entity configurations are applied using IEntityTypeConfiguration<T>
        // as features are added. Example:
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
