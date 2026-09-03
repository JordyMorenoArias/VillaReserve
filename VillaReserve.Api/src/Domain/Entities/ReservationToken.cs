using VillaReserve.Api.Domain.Enums;

namespace VillaReserve.Api.Domain.Entities;

/// <summary>
/// Secure cryptographic token hash providing access to reservation operations.
/// </summary>
public sealed class ReservationToken
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public ReservationTokenPurpose Purpose { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
