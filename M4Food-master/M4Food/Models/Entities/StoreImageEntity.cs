using SQLite;

namespace M4Food.Models.Entities;

[Table("store_images")]
public class StoreImageEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty; // Cloudinary public_id or custom ID
    
    [Indexed]
    public string StoreId { get; set; } = string.Empty;
    
    public string? RemoteUrl { get; set; } // Cloudinary URL
    
    public string? LocalPath { get; set; } // Local cache path
    
    public string? CloudinaryPublicId { get; set; } // Cloudinary public_id
    
    public int Width { get; set; }
    
    public int Height { get; set; }
    
    public long FileSize { get; set; } // File size in bytes
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastSyncedAt { get; set; }
    
    public bool IsUploaded { get; set; } = false; // Whether uploaded to Cloudinary
}

