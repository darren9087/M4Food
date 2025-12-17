using SQLite;

namespace M4Food.Models.Entities;

[Table("routes")]
public class RouteEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [Indexed]
    public string FromLocation { get; set; } = string.Empty;
    
    [Indexed]
    public string ToLocation { get; set; } = string.Empty;
    
    public string? RouteData { get; set; } // Route data in JSON format
    
    public double Distance { get; set; } // Distance in meters
    
    public int Duration { get; set; } // Duration in seconds
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
}

