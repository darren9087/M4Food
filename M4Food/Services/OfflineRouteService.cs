using M4Food.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace M4Food.Services;

public class OfflineRouteService : IOfflineRouteService
{
    private readonly ILocalCacheService _localCache;
    private readonly ILogger<OfflineRouteService>? _logger;

    public OfflineRouteService(
        ILocalCacheService localCache,
        ILogger<OfflineRouteService>? logger = null)
    {
        _localCache = localCache;
        _logger = logger;
    }

    public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate distance between two points
        const double R = 6371000; // Earth radius in meters
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;
        
        return distance;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    public string GenerateRoutePath(double fromLat, double fromLon, double toLat, double toLon)
    {
        // Generate a simple route path (straight line for now)
        // In production, you might want to use a more sophisticated algorithm
        // or integrate with OSRM for better routing
        
        var routePoints = new List<object>
        {
            new { lat = fromLat, lon = fromLon },
            new { lat = toLat, lon = toLon }
        };
        
        var routeData = new
        {
            type = "FeatureCollection",
            features = new[]
            {
                new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates = routePoints.Select(p => new[] { ((dynamic)p).lon, ((dynamic)p).lat }).ToArray()
                    },
                    properties = new { }
                }
            }
        };
        
        return JsonSerializer.Serialize(routeData);
    }

    public async Task<RouteDto> CalculateRouteAsync(double fromLat, double fromLon, double toLat, double toLon)
    {
        try
        {
            _logger?.LogInformation("Calculating route: From ({FromLat}, {FromLon}) to ({ToLat}, {ToLon})",
                fromLat, fromLon, toLat, toLon);
            
            // Calculate distance using Haversine formula
            var distance = CalculateDistance(fromLat, fromLon, toLat, toLon);
            
            // Estimate duration (assuming average walking speed of 5 km/h)
            var walkingSpeedMps = 5000.0 / 3600.0; // meters per second
            var duration = (int)(distance / walkingSpeedMps);
            
            // Generate route path
            var routeData = GenerateRoutePath(fromLat, fromLon, toLat, toLon);
            
            var route = new RouteDto
            {
                FromLocation = $"{fromLat},{fromLon}",
                ToLocation = $"{toLat},{toLon}",
                RouteData = routeData,
                Distance = distance,
                Duration = duration
            };
            
            // Save to cache
            await _localCache.SaveRouteAsync(route);
            
            _logger?.LogInformation("Route calculated: Distance={Distance}m, Duration={Duration}s", distance, duration);
            
            return route;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating route");
            throw;
        }
    }

    public async Task<RouteDto> CalculateRouteBetweenStoresAsync(string fromStoreId, string toStoreId)
    {
        try
        {
            // Get store information
            var fromStore = await _localCache.GetStoreAsync(fromStoreId);
            var toStore = await _localCache.GetStoreAsync(toStoreId);
            
            if (fromStore == null)
                throw new ArgumentException($"Store not found: {fromStoreId}", nameof(fromStoreId));
            
            if (toStore == null)
                throw new ArgumentException($"Store not found: {toStoreId}", nameof(toStoreId));
            
            // Check if route already exists in cache
            var fromLocation = $"{fromStore.Latitude},{fromStore.Longitude}";
            var toLocation = $"{toStore.Latitude},{toStore.Longitude}";
            
            var cachedRoute = await _localCache.GetRouteAsync(fromLocation, toLocation);
            if (cachedRoute != null)
            {
                _logger?.LogInformation("Using cached route");
                return cachedRoute;
            }
            
            // Calculate new route
            return await CalculateRouteAsync(
                fromStore.Latitude, fromStore.Longitude,
                toStore.Latitude, toStore.Longitude
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating route between stores");
            throw;
        }
    }
}

