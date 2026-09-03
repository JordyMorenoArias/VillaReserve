using FluentValidation;
using VillaReserve.Api.API.Middleware;

namespace VillaReserve.Api.API.Extensions;

/// <summary>
/// Extension methods for registering API-layer services:
/// controllers, OpenAPI, Scalar, exception handling, validation.
/// </summary>
public static class ApiServiceExtensions
{
    /// <summary>
    /// Registers all API-layer services into the DI container.
    /// </summary>
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();

        services.AddOpenApi();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Register all FluentValidation validators from this assembly.
        // Validators for each feature are co-located in their feature folder
        // and discovered automatically here.
        services.AddValidatorsFromAssemblyContaining<GlobalExceptionHandler>(
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        return services;
    }

    /// <summary>
    /// Configures the HTTP request pipeline for the API layer.
    /// </summary>
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapControllers();

        return app;
    }
}
