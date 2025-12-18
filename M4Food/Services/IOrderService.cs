using M4Food.Views;

namespace M4Food.Services;

/// <summary>
/// Service interface for managing orders with Firebase Realtime Database backend.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order and saves it to the backend.
    /// </summary>
    Task<Order> CreateOrderAsync(Order order);

    /// <summary>
    /// Gets all orders for the current user.
    /// </summary>
    Task<List<Order>> GetOrdersAsync();

    /// <summary>
    /// Gets a specific order by ID.
    /// </summary>
    Task<Order?> GetOrderAsync(string orderId);

    /// <summary>
    /// Updates the status of an order.
    /// </summary>
    Task<bool> UpdateOrderStatusAsync(string orderId, string newStatus);

    /// <summary>
    /// Cancels an order (sets status to "Cancelled").
    /// </summary>
    Task<bool> CancelOrderAsync(string orderId);
}

