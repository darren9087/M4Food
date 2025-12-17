namespace M4Food.Services;

/// <summary>
/// Cloudinary configuration class
/// You can read these values from appsettings.json, environment variables, or SecureStorage
/// </summary>
public class CloudinaryConfig
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

