namespace VillaReserve.Api.Domain.Enums;

/// <summary>
/// Status of synchronization with an external calendar service.
/// </summary>
public enum CalendarSyncStatus
{
    Pending,
    Synced,
    Failed
}
