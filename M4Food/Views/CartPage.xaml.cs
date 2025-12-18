using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;

namespace M4Food.Views
{
    public partial class CartPage : ContentPage
    {
        public CartPage()
        {
            InitializeComponent();
            this.BindingContext = CartService.Current;
            this.Appearing += CartPage_Appearing;
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

        private async void OnCheckoutClicked(object sender, EventArgs e)
        {
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

                await DisplayAlert("Order Confirmed!",
                    $"Order #{newOrder.OrderId} placed successfully!\n\nTotal: RM {total:F2}\nEstimated delivery: 30-45 minutes",
                    "OK");

                CartService.Current.ClearCart();
                UpdateTotal();

                await Navigation.PopAsync();

                var ordersPage = new OrdersPage();
                ordersPage.AddNewOrder(newOrder);
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
                await DisplayAlert("Checkout Error",
                    $"Failed to process order: {ex.Message}",
                    "OK");
            }
        }
    }
}