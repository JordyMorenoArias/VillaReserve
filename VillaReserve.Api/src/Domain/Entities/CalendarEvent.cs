using VillaReserve.Api.Domain.Enums;

namespace VillaReserve.Api.Domain.Entities;

/// <summary>
/// Represents the synchronization link between a VillaReserve reservation and an external calendar event.
/// </summary>
public sealed class CalendarEvent
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public CalendarProvider Provider { get; set; }
    public string ExternalEventId { get; set; } = string.Empty;
    public CalendarSyncStatus SyncStatus { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
