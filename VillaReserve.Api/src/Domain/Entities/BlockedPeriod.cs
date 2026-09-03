namespace VillaReserve.Api.Domain.Entities;

/// <summary>
/// Represents a manually configured period during which the villa cannot be reserved.
/// </summary>
public sealed class BlockedPeriod
{
    public Guid Id { get; set; }
    public DateTimeOffset StartDateTime { get; set; }
    public DateTimeOffset EndDateTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint RowVersion { get; set; }

    public User CreatedByUser { get; set; } = null!;
}
