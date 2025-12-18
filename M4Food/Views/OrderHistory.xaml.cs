using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using M4Food.Services;
#if ANDROID
using M4Food;
#endif

namespace M4Food.Views
{
    public partial class OrdersPage : ContentPage
    {
        private bool _showActiveOrders = true;
        private List<Order> _allOrders = new List<Order>();
        private readonly IOrderService _orderService;

        public OrdersPage()
        {
            InitializeComponent();

            // Get OrderService from DI
            _orderService = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService(typeof(IOrderService)) as IOrderService
                ?? throw new InvalidOperationException("IOrderService not registered.");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                // Show loading indicator
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                OrdersCollectionView.IsVisible = false;

                // Load orders from Firebase
                _allOrders = await _orderService.GetOrdersAsync();

                foreach (var order in _allOrders)
                {
                    System.Diagnostics.Debug.WriteLine($"Order {order.OrderId} Store: {order.StoreDisplay}");
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {_allOrders.Count} orders");

                // Refresh the current view
                if (_showActiveOrders)
                {
                    ShowActiveOrders();
                }
                else
                {
                    ShowPastOrders();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex.Message}");
                await DisplayAlert("Error", "Failed to load orders. Please try again.", "OK");
            }
            finally
            {
                // Hide loading indicator
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                OrdersCollectionView.IsVisible = true;
            }
        }

        /// <summary>
        /// Called from CartPage after a new order is placed.
        /// Adds the order to the local list and refreshes the view.
        /// </summary>
        public void AddNewOrder(Order newOrder)
        {
            newOrder.TotalPrice = newOrder.Items.Sum(item => item.Quantity * item.Price);
            _allOrders.Insert(0, newOrder);

           

            if (_showActiveOrders)
            {
                ShowActiveOrders();
            }
            else
            {
                ShowPastOrders();
            }
        }

        private void ShowActiveOrders()
        {
            _showActiveOrders = true;
            ActiveIndicator.IsVisible = true;
            PastIndicator.IsVisible = false;

            ActiveOrdersLabel.TextColor = Color.FromArgb("#1A1A1A");
            PastOrdersLabel.TextColor = Color.FromArgb("#999999");

            var activeOrders = _allOrders
                .Where(o => o.Status == "Processing" || o.Status == "Pending")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (activeOrders.Count == 0)
            {
                OrdersCollectionView.ItemsSource = new List<Order>();
                ShowEmptyState("No Active Orders");
            }
            else
            {
                OrdersCollectionView.ItemsSource = activeOrders;
                OrdersCollectionView.EmptyView = null;
            }
        }

        private void ShowPastOrders()
        {
            _showActiveOrders = false;
            ActiveIndicator.IsVisible = false;
            PastIndicator.IsVisible = true;

            ActiveOrdersLabel.TextColor = Color.FromArgb("#999999");
            PastOrdersLabel.TextColor = Color.FromArgb("#1A1A1A");

            var pastOrders = _allOrders
                .Where(o => o.Status == "Delivered" || o.Status == "Cancelled" || o.Status == "Completed")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (pastOrders.Count == 0)
            {
                OrdersCollectionView.ItemsSource = new List<Order>();
                ShowEmptyState("No Past Orders");
            }
            else
            {
                OrdersCollectionView.ItemsSource = pastOrders;
                OrdersCollectionView.EmptyView = null;
            }
        }

        private void ShowEmptyState(string message)
        {
            var emptyView = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 20,
                Margin = new Thickness(0, 50, 0, 0)
            };

            emptyView.Children.Add(new Label
            {
                Text = "📋",
                FontSize = 60,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb("#CCCCCC")
            });

            emptyView.Children.Add(new Label
            {
                Text = message,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#999999"),
                HorizontalOptions = LayoutOptions.Center
            });

            emptyView.Children.Add(new Label
            {
                Text = "Your orders will appear here",
                FontSize = 14,
                TextColor = Color.FromArgb("#BBBBBB"),
                HorizontalOptions = LayoutOptions.Center
            });

            OrdersCollectionView.EmptyView = emptyView;
        }

        private void OnActiveOrdersTapped(object sender, EventArgs e)
        {
            ShowActiveOrders();
        }

        private void OnPastOrdersTapped(object sender, EventArgs e)
        {
            ShowPastOrders();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnConfirmReceivedClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string orderId)
            {
                var confirm = await DisplayAlert(
                    "Confirm Received",
                    "Have you received your order?",
                    "Yes, Received",
                    "Cancel");

                if (!confirm) return;

                try
                {
                    var success = await _orderService.UpdateOrderStatusAsync(orderId, "Completed");
                    
                    if (success)
                    {
                        // Update local list
                        var order = _allOrders.FirstOrDefault(o => o.OrderId == orderId);
                        if (order != null)
                        {
                            order.Status = "Completed";
                        }

                        await DisplayAlert("Success", "Order marked as completed!", "OK");
                        
                        // Refresh the view
                        ShowActiveOrders();

#if ANDROID
                        NotificationHelper.ShowNotification(
                            "Order Completed",
                            $"Thank you! Order #{orderId} has been completed."
                        );
#endif
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to update order status.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error confirming order: {ex.Message}");
                    await DisplayAlert("Error", "Failed to update order. Please try again.", "OK");
                }
            }
        }

        private async void OnCancelOrderClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string orderId)
            {
                var confirm = await DisplayAlert(
                    "Cancel Order",
                    "Are you sure you want to cancel this order?",
                    "Yes, Cancel",
                    "No");

                if (!confirm) return;

                try
                {
                    var success = await _orderService.CancelOrderAsync(orderId);
                    
                    if (success)
                    {
                        // Update local list
                        var order = _allOrders.FirstOrDefault(o => o.OrderId == orderId);
                        if (order != null)
                        {
                            order.Status = "Cancelled";
                        }

                        await DisplayAlert("Cancelled", "Order has been cancelled.", "OK");
                        
                        // Refresh the view
                        ShowActiveOrders();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to cancel order.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cancelling order: {ex.Message}");
                    await DisplayAlert("Error", "Failed to cancel order. Please try again.", "OK");
                }
            }
        }
    }

    public class OrderItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }

    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public ObservableCollection<OrderItem> Items { get; set; } = new ObservableCollection<OrderItem>();
        public double TotalPrice { get; set; }

        public string StoreDisplay
        {
            get
            {
             
                if (Items == null || Items.Count == 0)
                {
                    return "No Store Info";
                }
                return Items[0].StoreName;
            }
        }

        /// <summary>
        /// Returns true if the order is still active (can be confirmed or cancelled)
        /// </summary>
        public bool IsActive => Status == "Processing" || Status == "Pending";

        public Color StatusColor
        {
            get
            {
                return Status switch
                {
                    "Processing" or "Pending" => Color.FromArgb("#FFA500"),
                    "Delivered" or "Completed" => Color.FromArgb("#4CAF50"),
                    "Cancelled" => Color.FromArgb("#FF4B4B"),
                    _ => Color.FromArgb("#666666")
                };
            }
        }

        public string ItemsSummary
        {
            get
            {
                if (Items == null || Items.Count == 0)
                    return "No items";

                var itemCount = Items.Sum(i => i.Quantity);
                var firstTwoItems = Items.Take(2);
                var itemNames = string.Join(", ", firstTwoItems.Select(i => $"{i.Quantity}x {i.Name}"));

                var result = $"{itemCount} item{(itemCount > 1 ? "s" : "")}: {itemNames}";

                if (Items.Count > 2)
                    result += "...";

                return result;
            }
        }
    }
}
