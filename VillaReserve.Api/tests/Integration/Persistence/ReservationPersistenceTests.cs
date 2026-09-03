using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VillaReserve.Api.Domain.Entities;
using VillaReserve.Api.Domain.Enums;
using VillaReserve.Api.Infrastructure.Persistence;
using VillaReserve.Integration.Tests.Infrastructure;

namespace VillaReserve.Integration.Tests.Persistence;

[Collection("Integration")]
public sealed class ReservationPersistenceTests : IClassFixture<VillaReserveWebApplicationFactory>
{
    private readonly VillaReserveWebApplicationFactory _factory;

    public ReservationPersistenceTests(VillaReserveWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OverlappingReservations_WithActiveStatuses_ThrowsExceptionDueToExclusionConstraint()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseTime = DateTimeOffset.UtcNow.AddDays(10);
        var resA = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "John Doe",
            CustomerPhone = "+1234567890",
            StartDateTime = baseTime,
            EndDateTime = baseTime.AddHours(5),
            Status = ReservationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var resB = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Jane Doe",
            CustomerPhone = "+0987654321",
            StartDateTime = baseTime.AddHours(4),
            EndDateTime = baseTime.AddHours(8),
            Status = ReservationStatus.Confirmed,
            ConfirmedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Reservations.Add(resA);
        await context.SaveChangesAsync();

        context.Reservations.Add(resB);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AdjacentReservations_AreBothPersistedSuccessfully()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseTime = DateTimeOffset.UtcNow.AddDays(20);
        var resA = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Adjacent One",
            CustomerPhone = "+1111111111",
            StartDateTime = baseTime,
            EndDateTime = baseTime.AddHours(5),
            Status = ReservationStatus.Confirmed,
            ConfirmedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var resB = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Adjacent Two",
            CustomerPhone = "+2222222222",
            StartDateTime = baseTime.AddHours(5),
            EndDateTime = baseTime.AddHours(9),
            Status = ReservationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Reservations.AddRange(resA, resB);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(ReservationStatus.Rejected)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Expired)]
    public async Task InactiveReservation_DoesNotBlockOverlappingActiveReservation(ReservationStatus inactiveStatus)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseTime = DateTimeOffset.UtcNow.AddDays(30 + (int)inactiveStatus * 5);
        var inactiveRes = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Inactive Res",
            CustomerPhone = "+3333333333",
            StartDateTime = baseTime,
            EndDateTime = baseTime.AddHours(6),
            Status = inactiveStatus,
            CancelledAt = inactiveStatus == ReservationStatus.Cancelled ? DateTimeOffset.UtcNow : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var activeRes = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Active Res",
            CustomerPhone = "+4444444444",
            StartDateTime = baseTime.AddHours(2),
            EndDateTime = baseTime.AddHours(8),
            Status = ReservationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Reservations.Add(inactiveRes);
        await context.SaveChangesAsync();

        context.Reservations.Add(activeRes);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Reservation_WithEndBeforeOrEqualStart_ViolatesCheckConstraint()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseTime = DateTimeOffset.UtcNow.AddDays(50);
        var invalidRes = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Invalid Dates",
            CustomerPhone = "+5555555555",
            StartDateTime = baseTime,
            EndDateTime = baseTime.AddHours(-1),
            Status = ReservationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Reservations.Add(invalidRes);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Reservation_WithZeroGuestCount_ViolatesCheckConstraint()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baseTime = DateTimeOffset.UtcNow.AddDays(60);
        var invalidRes = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerName = "Invalid Guests",
            CustomerPhone = "+6666666666",
            GuestCount = 0,
            StartDateTime = baseTime,
            EndDateTime = baseTime.AddHours(4),
            Status = ReservationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.Reservations.Add(invalidRes);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
