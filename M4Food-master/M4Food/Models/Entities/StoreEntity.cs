using SQLite;

namespace M4Food.Models.Entities;

[Table("stores")]
public class StoreEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    public double Latitude { get; set; }
    
    public double Longitude { get; set; }
    
    public string? Description { get; set; }
    
    public string? Phone { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Used for sync tracking
    public DateTime? LastSyncedAt { get; set; }
}

