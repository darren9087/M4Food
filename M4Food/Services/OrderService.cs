using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using M4Food.Models.DTOs;
using M4Food.Views;
using Plugin.Firebase.Auth;

namespace M4Food.Services;

/// <summary>
/// Order service implementation using Firebase Realtime Database with local SQLite caching.
/// Implements offline-first approach: reads from local cache first, syncs with Firebase when online.
/// Orders are stored under: users/{userId}/orders/{orderId}
/// </summary>
public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalCacheService _localCacheService;
    private const string FirebaseDbUrl = "https://m4food-default-rtdb.asia-southeast1.firebasedatabase.app/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OrderService(ILocalCacheService localCacheService)
    {
        _localCacheService = localCacheService;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(FirebaseDbUrl)
        };
    }

    private async Task<string?> GetIdTokenAsync()
    {
        try
        {
            var user = CrossFirebaseAuth.Current.CurrentUser;
            if (user == null) return null;

            var tokenResult = await user.GetIdTokenResultAsync(false);
            return tokenResult.Token;
        }
        catch
        {
            return null;
        }
    }

    private string? GetUserId()
    {
        return CrossFirebaseAuth.Current.CurrentUser?.Uid;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User must be logged in to create an order.");

        // 1. Save to local cache first (offline-first)
        var cacheDto = MapToOrderCacheDto(order);
        cacheDto.NeedsSync = true; // Mark as needing sync
        await _localCacheService.SaveOrderAsync(userId, cacheDto);

        System.Diagnostics.Debug.WriteLine($"Order {order.OrderId} saved to local cache");

        // 2. Try to sync to Firebase
        try
        {
            var idToken = await GetIdTokenAsync();
            if (!string.IsNullOrEmpty(idToken))
            {
                var orderDto = MapToFirebaseDto(order);
                var response = await _httpClient.PutAsJsonAsync(
                    $"users/{userId}/orders/{order.OrderId}.json?auth={idToken}",
                    orderDto,
                    JsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    // Mark as synced
                    await _localCacheService.MarkOrderSyncedAsync(order.OrderId);
                    System.Diagnostics.Debug.WriteLine($"Order {order.OrderId} synced to Firebase");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to sync order to Firebase: {response.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing order to Firebase (will retry later): {ex.Message}");
            // Order is still saved locally, will sync later
        }

        return order;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return new List<Order>();

        // 1. Load from local cache first (instant, works offline)
        var cachedOrders = await _localCacheService.GetOrdersByUserAsync(userId);
        var orders = cachedOrders.Select(MapToOrder).ToList();
        
        System.Diagnostics.Debug.WriteLine($"Loaded {orders.Count} orders from local cache");

        // 2. Try to fetch from Firebase and update cache (background sync)
        _ = SyncOrdersFromFirebaseAsync(userId);

        return orders;
    }

    /// <summary>
    /// Background sync: fetches orders from Firebase and updates local cache
    /// </summary>
    private async Task SyncOrdersFromFirebaseAsync(string userId)
    {
        try
        {
            var idToken = await GetIdTokenAsync();
            if (string.IsNullOrEmpty(idToken))
                return;

            var response = await _httpClient.GetAsync($"users/{userId}/orders.json?auth={idToken}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch orders from Firebase: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(content) || content == "null")
                return;

            var ordersDict = JsonSerializer.Deserialize<Dictionary<string, OrderDto>>(content, JsonOptions);
            
            if (ordersDict == null)
                return;

            // Update local cache with Firebase data
            foreach (var dto in ordersDict.Values)
            {
                var cacheDto = new OrderCacheDto
                {
                    OrderId = dto.OrderId,
                    OrderDate = dto.OrderDate,
                    Status = dto.Status,
                    TotalPrice = dto.TotalPrice,
                    Items = dto.Items?.Select(i => new OrderItemCacheDto
                    {
                        Name = i.Name,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList() ?? new(),
                    LastSyncedAt = DateTime.UtcNow,
                    NeedsSync = false
                };

                await _localCacheService.SaveOrderAsync(userId, cacheDto);
            }

            System.Diagnostics.Debug.WriteLine($"Synced {ordersDict.Count} orders from Firebase to local cache");

            // Also sync any local orders that haven't been pushed yet
            await SyncUnsyncedOrdersToFirebaseAsync(userId, idToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing orders from Firebase: {ex.Message}");
        }
    }

    /// <summary>
    /// Push any locally-created orders that haven't been synced to Firebase
    /// </summary>
    private async Task SyncUnsyncedOrdersToFirebaseAsync(string userId, string idToken)
    {
        try
        {
            var unsyncedOrders = await _localCacheService.GetUnsyncedOrdersAsync(userId);
            
            foreach (var order in unsyncedOrders)
            {
                var orderDto = new OrderDto
                {
                    OrderId = order.OrderId,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    TotalPrice = order.TotalPrice,
                    Items = order.Items?.Select(i => new OrderItemDto
                    {
                        Name = i.Name,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"users/{userId}/orders/{order.OrderId}.json?auth={idToken}",
                    orderDto,
                    JsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    await _localCacheService.MarkOrderSyncedAsync(order.OrderId);
                    System.Diagnostics.Debug.WriteLine($"Synced local order {order.OrderId} to Firebase");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing unsynced orders: {ex.Message}");
        }
    }

    public async Task<Order?> GetOrderAsync(string orderId)
    {
        // Try local cache first
        var cached = await _localCacheService.GetOrderAsync(orderId);
        if (cached != null)
        {
            return MapToOrder(cached);
        }

        // Fall back to Firebase
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return null;

        var idToken = await GetIdTokenAsync();
        if (string.IsNullOrEmpty(idToken))
            return null;

        try
        {
            var response = await _httpClient.GetAsync($"users/{userId}/orders/{orderId}.json?auth={idToken}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content) || content == "null")
                return null;

            var dto = JsonSerializer.Deserialize<OrderDto>(content, JsonOptions);
            if (dto == null)
                return null;

            return MapDtoToOrder(dto);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting order: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(string orderId, string newStatus)
    {
        // 1. Update local cache first
        await _localCacheService.UpdateOrderStatusAsync(orderId, newStatus);
        System.Diagnostics.Debug.WriteLine($"Order {orderId} status updated to {newStatus} in local cache");

        // 2. Try to sync to Firebase
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return true; // Local update succeeded

        try
        {
            var idToken = await GetIdTokenAsync();
            if (string.IsNullOrEmpty(idToken))
                return true;

            var updateData = new { status = newStatus };
            var response = await _httpClient.PatchAsync(
                $"users/{userId}/orders/{orderId}.json?auth={idToken}",
                JsonContent.Create(updateData, options: JsonOptions));

            if (response.IsSuccessStatusCode)
            {
                await _localCacheService.MarkOrderSyncedAsync(orderId);
                System.Diagnostics.Debug.WriteLine($"Order {orderId} status synced to Firebase");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error syncing order status to Firebase: {ex.Message}");
            return true; // Local update succeeded, Firebase sync will happen later
        }
    }

    public async Task<bool> CancelOrderAsync(string orderId)
    {
        return await UpdateOrderStatusAsync(orderId, "Cancelled");
    }

    #region Mapping Helpers

    private static OrderCacheDto MapToOrderCacheDto(Order order)
    {
        return new OrderCacheDto
        {
            OrderId = order.OrderId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Items = order.Items?.Select(i => new OrderItemCacheDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList() ?? new(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Order MapToOrder(OrderCacheDto dto)
    {
        return new Order
        {
            OrderId = dto.OrderId,
            OrderDate = dto.OrderDate,
            Status = dto.Status,
            TotalPrice = dto.TotalPrice,
            Items = new System.Collections.ObjectModel.ObservableCollection<OrderItem>(
                dto.Items?.Select(i => new OrderItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.Price
                }) ?? Enumerable.Empty<OrderItem>())
        };
    }

    private static Order MapDtoToOrder(OrderDto dto)
    {
        return new Order
        {
            OrderId = dto.OrderId,
            OrderDate = dto.OrderDate,
            Status = dto.Status,
            TotalPrice = dto.TotalPrice,
            Items = new System.Collections.ObjectModel.ObservableCollection<OrderItem>(
                dto.Items?.Select(i => new OrderItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.Price
                }) ?? Enumerable.Empty<OrderItem>())
        };
    }

    private static OrderDto MapToFirebaseDto(Order order)
    {
        return new OrderDto
        {
            OrderId = order.OrderId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Items = order.Items?.Select(i => new OrderItemDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
    }

    #endregion
}

/// <summary>
/// DTO for Firebase storage - uses simple types for JSON serialization
/// </summary>
internal class OrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public double TotalPrice { get; set; }
    public List<OrderItemDto>? Items { get; set; }
}

internal class OrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double Price { get; set; }
}
