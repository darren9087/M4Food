namespace M4Food.Services;

/// <summary>
/// Service for caching map tiles offline
/// </summary>
public interface IMapTileCacheService
{
    /// <summary>
    /// Download and cache map tiles for a specific area
    /// </summary>
    /// <param name="centerLat">Center latitude</param>
    /// <param name="centerLon">Center longitude</param>
    /// <param name="zoomLevel">Zoom level (1-18)</param>
    /// <param name="radiusKm">Radius in kilometers to download tiles</param>
    Task DownloadAndCacheTilesAsync(double centerLat, double centerLon, int zoomLevel, double radiusKm = 5.0);
    
    /// <summary>
    /// Get cached tile file path
    /// </summary>
    /// <param name="x">Tile X coordinate</param>
    /// <param name="y">Tile Y coordinate</param>
    /// <param name="z">Zoom level</param>
    /// <returns>Local file path if cached, null otherwise</returns>
    Task<string?> GetTilePathAsync(int x, int y, int z);
    
    /// <summary>
    /// Check if a tile is cached
    /// </summary>
    Task<bool> IsTileCachedAsync(int x, int y, int z);
    
    /// <summary>
    /// Get tile URL for downloading (can be used by frontend)
    /// </summary>
    string GetTileUrl(int x, int y, int z);
    
    /// <summary>
    /// Clear old cached tiles
    /// </summary>
    /// <param name="olderThanDays">Clear tiles older than specified days</param>
    Task ClearOldTilesAsync(int olderThanDays = 30);
    
    /// <summary>
    /// Get cache size in bytes
    /// </summary>
    Task<long> GetCacheSizeAsync();
}

