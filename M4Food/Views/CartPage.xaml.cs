using Microsoft.Maui.Controls;
using System;

namespace M4Food.Views
{
    public partial class CartPage : ContentPage
    {
        public CartPage()
        {
            InitializeComponent();

            // Set the BindingContext to the singleton instance
            this.BindingContext = CartService.Current;

            // Update the total price every time the page appears
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

        // Custom back button logic
        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnCheckoutClicked(object sender, EventArgs e)
        {
            if (CartService.Current.Items.Count == 0)
            {
                await DisplayAlert("Checkout", "Your cart is empty. Please add some items first.", "OK");
            }
            else
            {
                await DisplayAlert("Checkout", $"Total: RM {CartService.Current.GetTotal():F2}. Proceeding to payment...", "OK");

                // Clear the cart after successful checkout simulation
                CartService.Current.ClearCart();
                await Navigation.PopAsync(); // Navigate back after checkout
            }
        }
    }
}