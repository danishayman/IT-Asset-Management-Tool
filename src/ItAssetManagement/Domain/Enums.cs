namespace ItAssetManagement.Domain;

/// <summary>Lifecycle state of a piece of hardware.</summary>
public enum AssetStatus
{
    InUse,
    Available,
    Repair,
    Retired
}

/// <summary>The kind of change recorded in an <see cref="ActivityLog"/> row.</summary>
public enum ActivityAction
{
    Create,
    Update,
    Delete,
    Assign,
    Unassign
}

public enum TicketStatus
{
    Open,
    InProgress,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}
