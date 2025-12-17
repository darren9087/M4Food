namespace M4Food.Services;

public interface ICloudinaryService
{
    /// <summary>
    /// Upload image to Cloudinary
    /// </summary>
    /// <param name="imagePath">Local image file path</param>
    /// <param name="folder">Cloudinary folder path (optional)</param>
    /// <param name="publicId">Custom public_id (optional)</param>
    /// <returns>Uploaded URL and public_id</returns>
    Task<(string Url, string PublicId)> UploadImageAsync(string imagePath, string? folder = null, string? publicId = null);
    
    /// <summary>
    /// Upload image stream to Cloudinary
    /// </summary>
    Task<(string Url, string PublicId)> UploadImageStreamAsync(Stream imageStream, string fileName, string? folder = null, string? publicId = null);
    
    /// <summary>
    /// Delete image from Cloudinary
    /// </summary>
    Task<bool> DeleteImageAsync(string publicId);
    
    /// <summary>
    /// Get optimized image URL with transformation parameters
    /// </summary>
    string GetOptimizedUrl(string publicId, int? width = null, int? height = null, string? format = null);
}

