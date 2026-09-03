namespace VillaReserve.Api.Domain.Entities;

/// <summary>
/// Represents an immutable audit record of system changes.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
}
