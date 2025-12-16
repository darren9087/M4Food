using M4Food.Models.DTOs;

namespace M4Food.Services;

public interface IProfileService
{
    /// <summary>
    /// Save profile locally, optionally uploading avatar to Cloudinary.
    /// </summary>
    Task<UserProfileDto> SaveProfileAsync(
        UserProfileDto profile,
        Stream? avatarStream = null,
        string? avatarFileName = null,
        string? contentType = null);

    Task<UserProfileDto?> GetProfileAsync(string userId);

    Task DeleteProfileAsync(string userId);

    // Convenience helpers to access cached profile directly
    Task<UserProfileDto?> GetUserProfileAsync(string userId);
    Task SaveUserProfileAsync(UserProfileDto profile);
}


