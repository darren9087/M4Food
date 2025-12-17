namespace M4Food.Models.DTOs;

public class StoreImageDto
{
    public string Id { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string? RemoteUrl { get; set; }
    public string? LocalPath { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
    public bool IsUploaded { get; set; }
}

