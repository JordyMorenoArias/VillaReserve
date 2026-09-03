namespace VillaReserve.Api.Domain.Enums;

/// <summary>
/// Represents the lifecycle states of a reservation.
/// </summary>
public enum ReservationStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled,
    Expired
}
