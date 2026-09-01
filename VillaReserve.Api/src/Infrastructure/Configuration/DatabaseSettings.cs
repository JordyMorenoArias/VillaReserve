using System.ComponentModel.DataAnnotations;

namespace VillaReserve.Api.Infrastructure.Configuration;

/// <summary>
/// Typed configuration for PostgreSQL database access.
/// Bound to the "Database" section of application settings.
/// Validated at startup; missing or empty values cause the application to fail fast.
/// </summary>
public sealed class DatabaseSettings
{
    /// <summary>The PostgreSQL connection string.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
