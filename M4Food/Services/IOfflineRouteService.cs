using M4Food.Models.DTOs;

namespace M4Food.Services;

/// <summary>
/// Service for calculating routes offline
/// </summary>
public interface IOfflineRouteService
{
    /// <summary>
    /// Calculate route between two points offline
    /// </summary>
    /// <param name="fromLat">Starting latitude</param>
    /// <param name="fromLon">Starting longitude</param>
    /// <param name="toLat">Destination latitude</param>
    /// <param name="toLon">Destination longitude</param>
    /// <returns>Route information</returns>
    Task<RouteDto> CalculateRouteAsync(double fromLat, double fromLon, double toLat, double toLon);
    
    /// <summary>
    /// Calculate route using store IDs
    /// </summary>
    Task<RouteDto> CalculateRouteBetweenStoresAsync(string fromStoreId, string toStoreId);
    
    /// <summary>
    /// Calculate distance between two points (Haversine formula)
    /// </summary>
    double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
    
    /// <summary>
    /// Generate simple route path (straight line or basic path)
    /// </summary>
    string GenerateRoutePath(double fromLat, double fromLon, double toLat, double toLon);
}

