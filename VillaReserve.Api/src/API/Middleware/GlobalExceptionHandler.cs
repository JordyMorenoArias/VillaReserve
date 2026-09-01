using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace VillaReserve.Api.API.Middleware;

/// <summary>
/// Centralized exception handler that maps unhandled exceptions to RFC 7807 ProblemDetails responses.
/// Registered via app.UseExceptionHandler() in Program.cs.
///
/// Security contract:
///   - Stack traces, connection strings, and internal details are NEVER included in responses.
///   - A correlation ID is included to help administrators trace logs.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            return await HandleValidationExceptionAsync(httpContext, validationException, cancellationToken);
        }

        return await HandleUnexpectedExceptionAsync(httpContext, exception, cancellationToken);
    }

    private async ValueTask<bool> HandleValidationExceptionAsync(
        HttpContext httpContext,
        ValidationException exception,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Validation failed: {Errors}", exception.Message);

        var problemDetails = new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7807"
        };

        foreach (var error in exception.Errors)
        {
            var key = error.PropertyName;
            if (!problemDetails.Errors.TryGetValue(key, out var existing))
            {
                problemDetails.Errors[key] = [error.ErrorMessage];
            }
            else
            {
                problemDetails.Errors[key] = [.. existing, error.ErrorMessage];
            }
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private async ValueTask<bool> HandleUnexpectedExceptionAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception. CorrelationId: {CorrelationId}",
            correlationId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred. Please try again later.",
            Type = "https://tools.ietf.org/html/rfc7807",
            Extensions = { ["correlationId"] = correlationId }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
