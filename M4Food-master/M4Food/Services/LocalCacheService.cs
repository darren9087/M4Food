using SQLite;
using M4Food.Models.DTOs;
using M4Food.Models.Entities;

namespace M4Food.Services;

public class LocalCacheService : ILocalCacheService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _databasePath;

    public LocalCacheService()
    {
        _databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "m4food.db3"
        );
    }

    private Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database != null)
            return Task.FromResult(_database);

        _database = new SQLiteAsyncConnection(
            _databasePath,
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache
        );
        return Task.FromResult(_database);
    }

    public async Task InitAsync()
    {
        var db = await GetDatabaseAsync();
        await db.CreateTableAsync<StoreEntity>();
        await db.CreateTableAsync<RouteEntity>();
        await db.CreateTableAsync<StoreImageEntity>();
    }

    #region Store Methods

    public async Task SaveStoreAsync(StoreDto store)
    {
        var db = await GetDatabaseAsync();
        var entity = new StoreEntity
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            Latitude = store.Latitude,
            Longitude = store.Longitude,
            Description = store.Description,
            Phone = store.Phone,
            CreatedAt = store.CreatedAt,
            UpdatedAt = store.UpdatedAt,
            LastSyncedAt = DateTime.UtcNow
        };
        await db.InsertOrReplaceAsync(entity);
    }

    public async Task SaveStoresAsync(IEnumerable<StoreDto> stores)
    {
        var db = await GetDatabaseAsync();
        var entities = stores.Select(s => new StoreEntity
        {
            Id = s.Id,
            Name = s.Name,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Description = s.Description,
            Phone = s.Phone,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            LastSyncedAt = DateTime.UtcNow
        });
        await db.InsertAllAsync(entities, "OR REPLACE");
    }

    public async Task<StoreDto?> GetStoreAsync(string id)
    {
        var db = await GetDatabaseAsync();
        var entity = await db.Table<StoreEntity>()
            .FirstOrDefaultAsync(s => s.Id == id);
        
        if (entity == null)
            return null;

        return new StoreDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Address = entity.Address,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Description = entity.Description,
            Phone = entity.Phone,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<IEnumerable<StoreDto>> GetStoresAsync()
    {
        var db = await GetDatabaseAsync();
        var entities = await db.Table<StoreEntity>().ToListAsync();
        
        return entities.Select(e => new StoreDto
        {
            Id = e.Id,
            Name = e.Name,
            Address = e.Address,
            Latitude = e.Latitude,
            Longitude = e.Longitude,
            Description = e.Description,
            Phone = e.Phone,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });
    }

    public async Task DeleteStoreAsync(string id)
    {
        var db = await GetDatabaseAsync();
        await db.DeleteAsync<StoreEntity>(id);
    }

    #endregion

    #region Route Methods

    public async Task SaveRouteAsync(RouteDto route)
    {
        var db = await GetDatabaseAsync();
        var existing = await db.Table<RouteEntity>()
            .FirstOrDefaultAsync(r => 
                r.FromLocation == route.FromLocation && 
                r.ToLocation == route.ToLocation);

        var entity = new RouteEntity
        {
            Id = existing?.Id ?? 0,
            FromLocation = route.FromLocation,
            ToLocation = route.ToLocation,
            RouteData = route.RouteData,
            Distance = route.Distance,
            Duration = route.Duration,
            LastUsedAt = DateTime.UtcNow
        };

        if (existing == null)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await db.InsertAsync(entity);
        }
        else
        {
            await db.UpdateAsync(entity);
        }
    }

    public async Task<RouteDto?> GetRouteAsync(string fromLocation, string toLocation)
    {
        var db = await GetDatabaseAsync();
        var entity = await db.Table<RouteEntity>()
            .FirstOrDefaultAsync(r => 
                r.FromLocation == fromLocation && 
                r.ToLocation == toLocation);

        if (entity == null)
            return null;

        return new RouteDto
        {
            FromLocation = entity.FromLocation,
            ToLocation = entity.ToLocation,
            RouteData = entity.RouteData,
            Distance = entity.Distance,
            Duration = entity.Duration
        };
    }

    public async Task<IEnumerable<RouteDto>> GetRoutesAsync()
    {
        var db = await GetDatabaseAsync();
        var entities = await db.Table<RouteEntity>().ToListAsync();
        
        return entities.Select(e => new RouteDto
        {
            FromLocation = e.FromLocation,
            ToLocation = e.ToLocation,
            RouteData = e.RouteData,
            Distance = e.Distance,
            Duration = e.Duration
        });
    }

    public async Task DeleteRouteAsync(string fromLocation, string toLocation)
    {
        var db = await GetDatabaseAsync();
        var entity = await db.Table<RouteEntity>()
            .FirstOrDefaultAsync(r => 
                r.FromLocation == fromLocation && 
                r.ToLocation == toLocation);
        
        if (entity != null)
            await db.DeleteAsync(entity);
    }

    public async Task ClearOldRoutesAsync(TimeSpan olderThan)
    {
        var db = await GetDatabaseAsync();
        var cutoffDate = DateTime.UtcNow - olderThan;
        var oldRoutes = await db.Table<RouteEntity>()
            .Where(r => r.LastUsedAt < cutoffDate || r.LastUsedAt == null)
            .ToListAsync();
        
        foreach (var route in oldRoutes)
        {
            await db.DeleteAsync(route);
        }
    }

    #endregion

    #region StoreImage Methods

    public async Task SaveImageAsync(StoreImageDto image)
    {
        var db = await GetDatabaseAsync();
        var entity = new StoreImageEntity
        {
            Id = image.Id,
            StoreId = image.StoreId,
            RemoteUrl = image.RemoteUrl,
            LocalPath = image.LocalPath,
            CloudinaryPublicId = image.CloudinaryPublicId,
            Width = image.Width,
            Height = image.Height,
            FileSize = image.FileSize,
            IsUploaded = image.IsUploaded,
            LastSyncedAt = DateTime.UtcNow
        };
        await db.InsertOrReplaceAsync(entity);
    }

    public async Task<StoreImageDto?> GetImageAsync(string imageId)
    {
        var db = await GetDatabaseAsync();
        var entity = await db.Table<StoreImageEntity>()
            .FirstOrDefaultAsync(i => i.Id == imageId);

        if (entity == null)
            return null;

        return new StoreImageDto
        {
            Id = entity.Id,
            StoreId = entity.StoreId,
            RemoteUrl = entity.RemoteUrl,
            LocalPath = entity.LocalPath,
            CloudinaryPublicId = entity.CloudinaryPublicId,
            Width = entity.Width,
            Height = entity.Height,
            FileSize = entity.FileSize,
            IsUploaded = entity.IsUploaded
        };
    }

    public async Task<IEnumerable<StoreImageDto>> GetImagesByStoreIdAsync(string storeId)
    {
        var db = await GetDatabaseAsync();
        var entities = await db.Table<StoreImageEntity>()
            .Where(i => i.StoreId == storeId)
            .ToListAsync();

        return entities.Select(e => new StoreImageDto
        {
            Id = e.Id,
            StoreId = e.StoreId,
            RemoteUrl = e.RemoteUrl,
            LocalPath = e.LocalPath,
            CloudinaryPublicId = e.CloudinaryPublicId,
            Width = e.Width,
            Height = e.Height,
            FileSize = e.FileSize,
            IsUploaded = e.IsUploaded
        });
    }

    public async Task DeleteImageAsync(string imageId)
    {
        var db = await GetDatabaseAsync();
        await db.DeleteAsync<StoreImageEntity>(imageId);
    }

    public async Task ClearUnusedImagesAsync()
    {
        var db = await GetDatabaseAsync();
        // Delete image records that haven't been synced for more than 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var unusedImages = await db.Table<StoreImageEntity>()
            .Where(i => i.LastSyncedAt < cutoffDate || i.LastSyncedAt == null)
            .ToListAsync();
        
        foreach (var image in unusedImages)
        {
            await db.DeleteAsync(image);
        }
    }

    #endregion
}

