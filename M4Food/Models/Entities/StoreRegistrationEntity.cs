using SQLite;

namespace M4Food.Models.Entities;

[Table("store_registrations")]
public class StoreRegistrationEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty; // Store ID or user ID

    public string StoreName { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? StoreImageUrl { get; set; }
    public string? StoreImageLocalPath { get; set; }
    public string? StoreImagePublicId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }
}

