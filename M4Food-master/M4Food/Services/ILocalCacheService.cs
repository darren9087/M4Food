using M4Food.Models.DTOs;

namespace M4Food.Services;

public interface ILocalCacheService
{
    /// <summary>
    /// Initialize database and create table structures
    /// </summary>
    Task InitAsync();
    
    // Store related methods
    Task SaveStoreAsync(StoreDto store);
    Task SaveStoresAsync(IEnumerable<StoreDto> stores);
    Task<StoreDto?> GetStoreAsync(string id);
    Task<IEnumerable<StoreDto>> GetStoresAsync();
    Task DeleteStoreAsync(string id);
    
    // Route related methods
    Task SaveRouteAsync(RouteDto route);
    Task<RouteDto?> GetRouteAsync(string fromLocation, string toLocation);
    Task<IEnumerable<RouteDto>> GetRoutesAsync();
    Task DeleteRouteAsync(string fromLocation, string toLocation);
    Task ClearOldRoutesAsync(TimeSpan olderThan);
    
    // StoreImage related methods
    Task SaveImageAsync(StoreImageDto image);
    Task<StoreImageDto?> GetImageAsync(string imageId);
    Task<IEnumerable<StoreImageDto>> GetImagesByStoreIdAsync(string storeId);
    Task DeleteImageAsync(string imageId);
    Task ClearUnusedImagesAsync();
}

