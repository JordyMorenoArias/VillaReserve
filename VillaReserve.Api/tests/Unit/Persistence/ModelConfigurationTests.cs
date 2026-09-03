using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using VillaReserve.Api.Domain.Entities;
using VillaReserve.Api.Domain.Enums;
using VillaReserve.Api.Infrastructure.Persistence;

namespace VillaReserve.Unit.Tests.Persistence;

public sealed class ModelConfigurationTests
{
    private readonly AppDbContext _context;
    private readonly IModel _designTimeModel;

    public ModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;

        _context = new AppDbContext(options);
        _designTimeModel = _context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void Model_ContainsAllExpectedEntities()
    {
        var entityTypes = _context.Model.GetEntityTypes().Select(e => e.ClrType).ToList();

        entityTypes.Should().Contain(typeof(User));
        entityTypes.Should().Contain(typeof(Reservation));
        entityTypes.Should().Contain(typeof(BlockedPeriod));
        entityTypes.Should().Contain(typeof(Notification));
        entityTypes.Should().Contain(typeof(CalendarEvent));
        entityTypes.Should().Contain(typeof(AuditLog));
        entityTypes.Should().Contain(typeof(ReservationToken));
    }

    [Theory]
    [InlineData(typeof(User), "users")]
    [InlineData(typeof(Reservation), "reservations")]
    [InlineData(typeof(BlockedPeriod), "blocked_periods")]
    [InlineData(typeof(Notification), "notifications")]
    [InlineData(typeof(CalendarEvent), "calendar_events")]
    [InlineData(typeof(AuditLog), "audit_logs")]
    [InlineData(typeof(ReservationToken), "reservation_tokens")]
    public void Model_MapsEntitiesToCorrectTableNames(Type entityType, string expectedTableName)
    {
        var entity = _context.Model.FindEntityType(entityType);

        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be(expectedTableName);
    }

    [Fact]
    public void Reservation_HasConfiguredCheckConstraints()
    {
        var entity = _designTimeModel.FindEntityType(typeof(Reservation))!;
        var checkConstraints = entity.GetCheckConstraints().Select(c => c.Name).ToList();

        checkConstraints.Should().Contain("ck_reservations_end_after_start");
        checkConstraints.Should().Contain("ck_reservations_guest_count");
        checkConstraints.Should().Contain("ck_reservations_confirmed_at");
        checkConstraints.Should().Contain("ck_reservations_cancelled_at");
    }

    [Fact]
    public void BlockedPeriod_HasConfiguredCheckConstraints()
    {
        var entity = _designTimeModel.FindEntityType(typeof(BlockedPeriod))!;
        var checkConstraints = entity.GetCheckConstraints().Select(c => c.Name).ToList();

        checkConstraints.Should().Contain("ck_blocked_periods_end_after_start");
    }

    [Fact]
    public void Notification_HasConfiguredCheckConstraints()
    {
        var entity = _designTimeModel.FindEntityType(typeof(Notification))!;
        var checkConstraints = entity.GetCheckConstraints().Select(c => c.Name).ToList();

        checkConstraints.Should().Contain("ck_notifications_read_at");
    }

    [Fact]
    public void Model_ConfiguresRequiredDeleteBehaviors()
    {
        var blockedPeriodEntity = _context.Model.FindEntityType(typeof(BlockedPeriod))!;
        var blockedPeriodFk = blockedPeriodEntity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        blockedPeriodFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);

        var notificationEntity = _context.Model.FindEntityType(typeof(Notification))!;
        var notificationFk = notificationEntity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        notificationFk.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        var auditLogEntity = _context.Model.FindEntityType(typeof(AuditLog))!;
        var auditLogFk = auditLogEntity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        auditLogFk.DeleteBehavior.Should().Be(DeleteBehavior.SetNull);

        var calendarEventEntity = _context.Model.FindEntityType(typeof(CalendarEvent))!;
        var calendarEventFk = calendarEventEntity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Reservation));
        calendarEventFk.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        var reservationTokenEntity = _context.Model.FindEntityType(typeof(ReservationToken))!;
        var tokenFk = reservationTokenEntity.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Reservation));
        tokenFk.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_ConfiguresUniqueIndexes()
    {
        var userEntity = _context.Model.FindEntityType(typeof(User))!;
        userEntity.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(User.Email)));

        var calendarEntity = _context.Model.FindEntityType(typeof(CalendarEvent))!;
        calendarEntity.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(CalendarEvent.ReservationId)));
        calendarEntity.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Count == 2
            && i.Properties.Any(p => p.Name == nameof(CalendarEvent.Provider))
            && i.Properties.Any(p => p.Name == nameof(CalendarEvent.ExternalEventId)));

        var tokenEntity = _context.Model.FindEntityType(typeof(ReservationToken))!;
        tokenEntity.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(ReservationToken.TokenHash)));
    }

    [Fact]
    public void AuditLog_OldValueAndNewValue_UseJsonbColumnType()
    {
        var entity = _context.Model.FindEntityType(typeof(AuditLog))!;

        entity.FindProperty(nameof(AuditLog.OldValue))!.GetColumnType().Should().Be("jsonb");
        entity.FindProperty(nameof(AuditLog.NewValue))!.GetColumnType().Should().Be("jsonb");
    }

    [Fact]
    public void Reservation_Status_HasStringValueConverter()
    {
        var entity = _context.Model.FindEntityType(typeof(Reservation))!;
        var statusProperty = entity.FindProperty(nameof(Reservation.Status))!;

        statusProperty.GetValueConverter().Should().NotBeNull();
        statusProperty.GetMaxLength().Should().Be(20);
    }

    [Theory]
    [InlineData(typeof(Reservation))]
    [InlineData(typeof(BlockedPeriod))]
    public void Model_ConfiguresOptimisticConcurrencyTokens(Type entityType)
    {
        var property = _context.Model.FindEntityType(entityType)!
            .FindProperty("RowVersion");

        property.Should().NotBeNull();
        property!.IsConcurrencyToken.Should().BeTrue();
        property.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
        property.GetColumnName().Should().Be("xmin");
    }
}
