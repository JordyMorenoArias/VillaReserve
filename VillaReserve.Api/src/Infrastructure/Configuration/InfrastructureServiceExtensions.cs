using Microsoft.EntityFrameworkCore;
using VillaReserve.Api.Infrastructure.Configuration;
using VillaReserve.Api.Infrastructure.Persistence;

namespace VillaReserve.Api.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure-layer services (EF Core, health checks).
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all infrastructure services: EF Core, PostgreSQL health check, and typed database settings.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseSettings>()
            .Bind(configuration.GetSection("Database"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Required configuration value 'Database:ConnectionString' is missing. " +
                "Set it via the 'Database__ConnectionString' environment variable or appsettings.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        return services;
    }
}
