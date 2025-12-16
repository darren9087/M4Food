using SQLite;

namespace M4Food.Models.Entities;

[Table("user_profiles")]
public class UserProfileEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty; // user id from auth

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Country { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarPublicId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }
}


