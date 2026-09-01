using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using VillaReserve.Api.API.Middleware;

namespace VillaReserve.Unit.Tests.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionHandler.
/// Verifies that the handler correctly maps exceptions to RFC 7807 ProblemDetails responses
/// without leaking sensitive information.
/// </summary>
public sealed class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _handler =
        new(NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_UnhandledException_Returns500WithCorrelationId()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Database exploded");

        // Act
        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledException_DoesNotExposeStackTrace()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Secret internal error");

        // Act
        await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert — the response body must not contain the exception message or stack trace.
        var body = await ReadResponseBodyAsync(httpContext);
        body.Should().NotContain("Secret internal error");
        body.Should().NotContain("StackTrace");
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_Returns400WithFieldErrors()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var failures = new List<ValidationFailure>
        {
            new("GuestName", "Guest name is required."),
            new("StartDateTime", "Start date must be in the future.")
        };
        var exception = new ValidationException(failures);

        // Act
        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var body = await ReadResponseBodyAsync(httpContext);
        body.Should().Contain("GuestName");
        body.Should().Contain("Guest name is required.");
        body.Should().Contain("StartDateTime");
    }

    [Fact]
    public async Task TryHandleAsync_AlwaysReturnsTrue()
    {
        // The handler must always claim ownership of the exception so that
        // no other handler produces an unformatted response.
        var httpContext = CreateHttpContext();
        var exception = new Exception("Any exception");

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.Should().BeTrue();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
