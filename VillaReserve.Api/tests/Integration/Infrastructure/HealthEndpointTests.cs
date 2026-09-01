using FluentAssertions;
using System.Net;

namespace VillaReserve.Integration.Tests.Infrastructure;

/// <summary>
/// Integration tests for the /health endpoint.
/// Verifies that the application correctly reports its health status
/// against a real PostgreSQL instance managed by Testcontainers.
/// </summary>
[Collection("Integration")]
public sealed class HealthEndpointTests : IClassFixture<VillaReserveWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(VillaReserveWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseIsReachable_Returns200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnknownRoute_Returns404NotFound()
    {
        // Verifies that unmatched routes return 404 (not 200 or 500),
        // and that the application does not have catch-all routes.
        var response = await _client.GetAsync("/this-route-does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
