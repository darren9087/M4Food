using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using M4Food.Services;

namespace M4Food.Views
{
    public partial class CartPage : ContentPage
    {
        private readonly IOrderService _orderService;

        public CartPage()
        {
            InitializeComponent();
            this.BindingContext = CartService.Current;
            this.Appearing += CartPage_Appearing;

            // Get OrderService from DI
            _orderService = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService(typeof(IOrderService)) as IOrderService
                ?? throw new InvalidOperationException("IOrderService not registered.");
        }

        /// <summary>
        /// Check if the device has internet connection
        /// </summary>
        private bool IsConnected()
        {
            return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        }

        private void CartPage_Appearing(object? sender, EventArgs e)
        {
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            double total = CartService.Current.GetTotal();
            TotalLabel.Text = $"RM {total:F2}";
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnDeleteItemClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string itemName)
            {
                var confirm = await DisplayAlert(
                    "Remove Item",
                    $"Remove '{itemName}' from cart?",
                    "Yes, Remove",
                    "Cancel");

                if (confirm)
                {
                    CartService.Current.RemoveItem(itemName);
                    UpdateTotal();
                }
            }
        }

        private void OnDeleteSwipeClicked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is string itemName)
            {
                CartService.Current.RemoveItem(itemName);
                UpdateTotal();
            }
        }

        private async void OnCheckoutClicked(object sender, EventArgs e)
        {
            // Check if device is online
            if (!IsConnected())
            {
                await DisplayAlert(
                    "Offline",
                    "You are currently offline. Please connect to the internet to checkout.",
                    "OK");
                return;
            }

            if (CartService.Current.Items.Count == 0)
            {
                await DisplayAlert("Checkout", "Your cart is empty. Please add some items first.", "OK");
                return;
            }

            var total = CartService.Current.GetTotal();
            var confirm = await DisplayAlert("Confirm Checkout",
                $"Total: RM {total:F2}\n\nProceed to payment?",
                "Yes, Checkout",
                "Cancel");

            if (!confirm)
                return;

            try
            {
                // Create the order object
                var newOrder = new Order
                {
                    OrderId = $"ORD{DateTime.Now:yyyyMMddHHmmss}",
                    OrderDate = DateTime.Now,
                    Status = "Processing",
                    Items = new ObservableCollection<OrderItem>(
                        CartService.Current.Items.Select(item => new OrderItem
                        {
                            Name = item.Name,
                            Quantity = item.Quantity,
                            Price = item.Price
                        })
                    ),
                    TotalPrice = total
                };

                newOrder.TotalPrice = newOrder.Items.Sum(item => item.Quantity * item.Price);

                // Save order to Firebase
                await _orderService.CreateOrderAsync(newOrder);

                await DisplayAlert("Order Confirmed!",
                    $"Order #{newOrder.OrderId} placed successfully!\n\nTotal: RM {total:F2}\nEstimated delivery: 30-45 minutes",
                    "OK");

                // Clear cart after successful order
                CartService.Current.ClearCart();
                UpdateTotal();

                // Navigate back and then to orders page
                await Navigation.PopAsync();

                // Navigate to OrdersPage to see the new order
                var ordersPage = new OrdersPage();
                ordersPage.AddNewOrder(newOrder); // Add to local list for immediate display
                await Navigation.PushAsync(ordersPage);

#if ANDROID
                NotificationHelper.ShowNotification(
                    "Order Confirmed!",
                    $"Your order #{newOrder.OrderId} has been placed successfully!"
                );
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Checkout error: {ex.Message}");
                await DisplayAlert("Checkout Error",
                    $"Failed to process order: {ex.Message}",
                    "OK");
            }
        }
    }
}
