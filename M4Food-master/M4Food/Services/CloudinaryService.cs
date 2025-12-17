using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace M4Food.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService>? _logger;

    public CloudinaryService(ILogger<CloudinaryService>? logger = null)
    {
        // TODO: Read these values from configuration file, environment variables, or SecureStorage
        // It's recommended to use SecureStorage for sensitive information
        // Example: var cloudName = await SecureStorage.GetAsync("cloudinary_cloud_name");
        
        // Temporarily using placeholders, you need to replace with actual Cloudinary configuration
        // Or read from CloudinaryConfig class
        var cloudName = "dswo5qtwy";      // Replace with your Cloudinary cloud name
        var apiKey = "518587147933721";            // Replace with your API key
        var apiSecret = "gg2R6WMRu8qrj9YiaHOcBta6LwQ";      // Replace with your API secret
        
        // Throw exception if configuration is missing
        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary configuration is missing. Please set CloudName, ApiKey, and ApiSecret.");
        }
        
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _logger = logger;
    }

    public async Task<(string Url, string PublicId)> UploadImageAsync(
        string imagePath, 
        string? folder = null, 
        string? publicId = null)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imagePath),
                Folder = folder ?? "m4food/stores",
                PublicId = publicId,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger?.LogInformation("Image uploaded successfully: {PublicId}", uploadResult.PublicId);
                return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
            }

            throw new Exception($"Upload failed with status: {uploadResult.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upload image: {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<(string Url, string PublicId)> UploadImageStreamAsync(
        Stream imageStream, 
        string fileName, 
        string? folder = null, 
        string? publicId = null)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, imageStream),
                Folder = folder ?? "m4food/stores",
                PublicId = publicId,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger?.LogInformation("Image stream uploaded successfully: {PublicId}", uploadResult.PublicId);
                return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
            }

            throw new Exception($"Upload failed with status: {uploadResult.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upload image stream: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        try
        {
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);
            
            if (result.Result == "ok")
            {
                _logger?.LogInformation("Image deleted successfully: {PublicId}", publicId);
                return true;
            }

            _logger?.LogWarning("Failed to delete image: {PublicId}, Result: {Result}", publicId, result.Result);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting image: {PublicId}", publicId);
            return false;
        }
    }

    public string GetOptimizedUrl(string publicId, int? width = null, int? height = null, string? format = null)
    {
        var transformation = new Transformation();
        
        if (width.HasValue)
            transformation = transformation.Width(width.Value);
        
        if (height.HasValue)
            transformation = transformation.Height(height.Value);

        var url = _cloudinary.Api.UrlImgUp
            .Transform(transformation)
            .BuildUrl(publicId);
        
        // If format is specified, replace the extension in the URL
        if (!string.IsNullOrEmpty(format))
        {
            var uri = new Uri(url);
            var pathWithoutExtension = uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('.'));
            url = $"{uri.Scheme}://{uri.Host}{pathWithoutExtension}.{format}";
        }
        
        return url;
    }
}

