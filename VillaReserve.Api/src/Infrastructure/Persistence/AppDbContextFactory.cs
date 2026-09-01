using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VillaReserve.Api.Infrastructure.Persistence;

namespace VillaReserve.Api.Infrastructure;

/// <summary>
/// Design-time factory for AppDbContext, used by the EF Core CLI (dotnet ef migrations add, etc.).
/// At design time, the full DI container and startup configuration are not available,
/// so the factory reads the connection string from an environment variable or falls back to
/// the local development default.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("Database__ConnectionString")
            ?? "Host=localhost;Port=5432;Database=villareserve;Username=villareserve_user;Password=change_me_in_dev";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
