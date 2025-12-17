using Microsoft.Extensions.Logging;

namespace M4Food.Services;

public class MapTileCacheService : IMapTileCacheService
{
    private readonly ILogger<MapTileCacheService>? _logger;
    private readonly string _cacheDirectory;
    private const string TileServerUrl = "https://tile.openstreetmap.org"; // Free OpenStreetMap tiles

    public MapTileCacheService(ILogger<MapTileCacheService>? logger = null)
    {
        _logger = logger;
        _cacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "map_tiles");
        
        // Create cache directory if it doesn't exist
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public string GetTileUrl(int x, int y, int z)
    {
        // OpenStreetMap tile URL format
        return $"{TileServerUrl}/{z}/{x}/{y}.png";
    }

    public async Task<string?> GetTilePathAsync(int x, int y, int z)
    {
        var tilePath = GetTileFilePath(x, y, z);
        
        if (File.Exists(tilePath))
        {
            return tilePath;
        }
        
        return null;
    }

    public async Task<bool> IsTileCachedAsync(int x, int y, int z)
    {
        var tilePath = GetTileFilePath(x, y, z);
        return File.Exists(tilePath);
    }

    public async Task DownloadAndCacheTilesAsync(double centerLat, double centerLon, int zoomLevel, double radiusKm = 5.0)
    {
        try
        {
            _logger?.LogInformation("Starting tile download for area: Lat={Lat}, Lon={Lon}, Zoom={Zoom}, Radius={Radius}km",
                centerLat, centerLon, zoomLevel, radiusKm);

            // Calculate tile coordinates for center point
            var (centerTileX, centerTileY) = LatLonToTile(centerLat, centerLon, zoomLevel);
            
            // Calculate how many tiles to download based on radius
            var tilesPerKm = Math.Pow(2, zoomLevel) / 111.0; // Approximate tiles per km
            var tilesRadius = (int)Math.Ceiling(radiusKm * tilesPerKm);
            
            var downloadedCount = 0;
            var skippedCount = 0;
            
            // Download tiles in a square around center
            for (int dx = -tilesRadius; dx <= tilesRadius; dx++)
            {
                for (int dy = -tilesRadius; dy <= tilesRadius; dy++)
                {
                    var tileX = centerTileX + dx;
                    var tileY = centerTileY + dy;
                    
                    // Check if tile is already cached
                    if (await IsTileCachedAsync(tileX, tileY, zoomLevel))
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    // Download tile
                    try
                    {
                        await DownloadTileAsync(tileX, tileY, zoomLevel);
                        downloadedCount++;
                        
                        // Small delay to avoid overwhelming the server
                        await Task.Delay(50);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to download tile: X={X}, Y={Y}, Z={Z}", tileX, tileY, zoomLevel);
                    }
                }
            }
            
            _logger?.LogInformation("Tile download completed: Downloaded={Downloaded}, Skipped={Skipped}",
                downloadedCount, skippedCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading tiles");
            throw;
        }
    }

    private async Task DownloadTileAsync(int x, int y, int z)
    {
        var tileUrl = GetTileUrl(x, y, z);
        var tilePath = GetTileFilePath(x, y, z);
        
        // Create directory if needed
        var tileDir = Path.GetDirectoryName(tilePath);
        if (!string.IsNullOrEmpty(tileDir) && !Directory.Exists(tileDir))
        {
            Directory.CreateDirectory(tileDir);
        }
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        var response = await httpClient.GetAsync(tileUrl);
        response.EnsureSuccessStatusCode();
        
        var imageData = await response.Content.ReadAsByteArrayAsync();
        
        await File.WriteAllBytesAsync(tilePath, imageData);
        
        _logger?.LogDebug("Downloaded tile: X={X}, Y={Y}, Z={Z}", x, y, z);
    }

    private string GetTileFilePath(int x, int y, int z)
    {
        return Path.Combine(_cacheDirectory, $"{z}", $"{x}", $"{y}.png");
    }

    private (int x, int y) LatLonToTile(double lat, double lon, int zoom)
    {
        var n = Math.Pow(2, zoom);
        var x = (int)((lon + 180.0) / 360.0 * n);
        var latRad = lat * Math.PI / 180.0;
        var y = (int)((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
        return (x, y);
    }

    public async Task ClearOldTilesAsync(int olderThanDays = 30)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var deletedCount = 0;
            
            if (!Directory.Exists(_cacheDirectory))
                return;
            
            var files = Directory.GetFiles(_cacheDirectory, "*.png", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    File.Delete(file);
                    deletedCount++;
                }
            }
            
            _logger?.LogInformation("Cleared {Count} old tiles", deletedCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing old tiles");
            throw;
        }
    }

    public async Task<long> GetCacheSizeAsync()
    {
        if (!Directory.Exists(_cacheDirectory))
            return 0;
        
        var files = Directory.GetFiles(_cacheDirectory, "*.*", SearchOption.AllDirectories);
        long totalSize = 0;
        
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            totalSize += fileInfo.Length;
        }
        
        return totalSize;
    }
}

