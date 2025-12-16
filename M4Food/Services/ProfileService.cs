using M4Food.Models.DTOs;

namespace M4Food.Services;

public class ProfileService : IProfileService
{
    private readonly ILocalCacheService _cache;
    private readonly ICloudinaryService _cloudinary;

    public ProfileService(ILocalCacheService cache, ICloudinaryService cloudinary)
    {
        _cache = cache;
        _cloudinary = cloudinary;
    }

    public async Task<UserProfileDto> SaveProfileAsync(
        UserProfileDto profile,
        Stream? avatarStream = null,
        string? avatarFileName = null,
        string? contentType = null)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            profile.Id = Guid.NewGuid().ToString();
        }

        if (avatarStream != null)
        {
            var fileName = string.IsNullOrWhiteSpace(avatarFileName) ? "avatar.jpg" : avatarFileName;
            var uploadResult = await _cloudinary.UploadImageStreamAsync(
                avatarStream,
                fileName,
                folder: "avatars",
                publicId: $"{profile.Id}-avatar");

            profile.AvatarUrl = uploadResult.Url;
            profile.AvatarPublicId = uploadResult.PublicId;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _cache.SaveUserProfileAsync(profile);
        return profile;
    }

    public Task<UserProfileDto?> GetProfileAsync(string userId) =>
        _cache.GetUserProfileAsync(userId);

    public async Task DeleteProfileAsync(string userId)
    {
        var existing = await _cache.GetUserProfileAsync(userId);
        if (!string.IsNullOrWhiteSpace(existing?.AvatarPublicId))
        {
            _ = await _cloudinary.DeleteImageAsync(existing.AvatarPublicId);
        }

        await _cache.DeleteUserProfileAsync(userId);
    }

    public Task<UserProfileDto?> GetUserProfileAsync(string userId) =>
        _cache.GetUserProfileAsync(userId);

    public Task SaveUserProfileAsync(UserProfileDto profile) =>
        _cache.SaveUserProfileAsync(profile);
}


