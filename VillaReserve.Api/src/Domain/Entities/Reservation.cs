using VillaReserve.Api.Domain.Enums;

namespace VillaReserve.Api.Domain.Entities;

/// <summary>
/// Represents a customer reservation or reservation request for the villa.
/// </summary>
public sealed class Reservation
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public int? GuestCount { get; set; }
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public ReservationStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public uint RowVersion { get; set; }

    public CalendarEvent? CalendarEvent { get; set; }
    public ICollection<ReservationToken> ReservationTokens { get; set; } = [];
}
