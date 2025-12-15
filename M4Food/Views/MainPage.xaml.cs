using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace M4Food.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // 1. Location Tap Handler
        private async void OnMapTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Location", "Opening map location selector...", "OK");
        }

        private async void OnCategoryTapped(object sender, EventArgs e)
        {
            if (sender is VisualElement element)
            {
                await element.ScaleTo(0.95, 100, Easing.CubicOut);
                await element.ScaleTo(1, 100, Easing.CubicIn);
            }

            string category = "Category";
            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                category = param;
            }

            await DisplayAlert("Category Filter", $"Filtering by: {category}", "OK");
        }

        // 3. Food Item Tap Handler (MODIFIED: Navigate to ItemDetailPage)
        private async void OnFoodItemTapped(object sender, EventArgs e)
        {
            if (sender is VisualElement element)
            {
                await element.ScaleTo(0.95, 100, Easing.CubicOut);
                await element.ScaleTo(1, 100, Easing.CubicIn);
            }

            string itemName = "Selected Item";
            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                itemName = param;
            }

            // Navigate to ItemDetailPage, and pass the item name
            await Navigation.PushAsync(new ItemDetailPage(itemName));
        }

        // 4. Free Delivery Promo
        private async void OnFreeDeliveryTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Free Delivery", "Code 'FREESHIP30' applied to your clipboard!", "OK");
        }

        // 5. Special Offer Promo
        private async void OnSpecialOfferingTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Special Offer", "50% Discount applied to Healthy Food category.", "OK");
        }

        // 6. See All Favorites
        private async void OnSeeAllTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Preferred", "Showing all your preferred items...", "OK");
        }

        // --- Bottom Navigation Handlers ---

        // 7. Home Button
        private async void OnHomeTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Home", "You are on the Home screen.", "OK");
        }

        // 8. Cart Button (MODIFIED: navigate to CartPage)
        private async void OnCartTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CartPage());
        }

        //9. Profile Navigation
        private async void OnAccountTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new M4Food.Views.ProfilePage());
        }
    }
}