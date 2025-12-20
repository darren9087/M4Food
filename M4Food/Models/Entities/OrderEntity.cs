using SQLite;
using System;

namespace M4Food.Models.Entities;

/// <summary>
/// SQLite entity for storing orders locally for offline access.
/// </summary>
[Table("orders")]
public class OrderEntity
{
    [PrimaryKey]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// The user ID who owns this order
    /// </summary>
    [Indexed]
    public string UserId { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public double TotalPrice { get; set; }
    public string? ReceivedImageUrl { get; set; }
    public string? ReceivedImageLocalPath { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>
    /// JSON serialized list of order items
    /// </summary>
    public string ItemsJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Flag to indicate if this order needs to be synced to the server
    /// </summary>
    public bool NeedsSync { get; set; } = false;
}

