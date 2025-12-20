using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;
using M4Food.ViewModels;

namespace M4Food.Views
{
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel? _viewModel;

        public MainPage()
        {
            InitializeComponent();
            
            // Set ViewModel as BindingContext (MVVM pattern)
            _viewModel = new MainPageViewModel();
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // App Lifecycle: Handle page appearing
            // This demonstrates App Lifecycle management (E2 requirement)

            // Update category button styles based on ViewModel's SelectedCategory
            if (_viewModel != null)
            {
                UpdateCategoryButtonStyle(_viewModel.SelectedCategory);
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // Refresh active orders count when page appears
                _ = _viewModel.LoadActiveOrdersCountAsync();
            }

            // Initialize FCM on Android (async operation)
            _ = InitializeFCMAsync();
        }

        private async Task InitializeFCMAsync()
        {
#if ANDROID
            try
            {
                var cloudMessaging = Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current;
                await cloudMessaging.CheckIfValidAsync();
                var token = await cloudMessaging.GetTokenAsync();
                System.Diagnostics.Debug.WriteLine($"FCM token (MainPage): {token}");
                await cloudMessaging.SubscribeToTopicAsync("daily");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to init FCM on MainPage: {ex.Message}");
            }
#else
            // FCM is only available on Android
            await Task.CompletedTask;
#endif
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update UI when ViewModel properties change (Data Binding support)
            if (e.PropertyName == nameof(MainPageViewModel.SelectedCategory) && _viewModel != null)
            {
                UpdateCategoryButtonStyle(_viewModel.SelectedCategory);
            }
        }

        private void UpdateCategoryButtonStyle(string selectedCategory)
        {
            var selectedColor = Color.FromArgb("#EBC656");
            var defaultTextColor = Color.FromArgb("#EBC656");
            var defaultBgColor = Colors.White;
            var selectedTextColor = Colors.White;

            Frame[] allFrames = { AllCategoryFrame, BreadCategoryFrame, CakeCategoryFrame, OthersCategoryFrame };
            Label[] allLabels = { AllCategoryLabel, BreadCategoryLabel, CakeCategoryLabel, OthersCategoryLabel };
            string[] categories = { "All", "Bread", "Cake", "Others" };

            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] == selectedCategory)
                {
                    allFrames[i].BackgroundColor = selectedColor;
                    allFrames[i].BorderColor = selectedColor;
                    allLabels[i].TextColor = selectedTextColor;
                }
                else
                {
                    allFrames[i].BackgroundColor = defaultBgColor;
                    allFrames[i].BorderColor = selectedColor;
                    allLabels[i].TextColor = defaultTextColor;
                }
            }
        }

        // Note: Cart badge is now handled by ViewModel through Data Binding
        // The ViewModel subscribes to CartService.CartChanged and updates properties
        // which are bound to the UI via {Binding IsCartBadgeVisible} and {Binding CartBadgeText}


        private async void OnMapTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Location", "Opening map location selector...", "OK");
        }

        // Note: Category selection is now handled by ViewModel's CategorySelectedCommand
        // This method is kept for backward compatibility but may not be used if XAML uses Command binding

        private async void OnFoodItemTapped(object sender, string itemName, string storeName)
        {
            await NavigateToItemDetail(sender, itemName, storeName);
        }

        private async void OnFoodItemTapped(object sender, EventArgs e)
        {
            string itemName = "Selected Item";
            string storeName = "Unknown Store";

            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                itemName = param;
            }
            else if (sender is BindableObject bindable && bindable.BindingContext is string contextName)
            {
                itemName = contextName;
            }

            await NavigateToItemDetail(sender, itemName, storeName);
        }

        private async void OnPreferredArtisanBreadTapped(object sender, EventArgs e)
        {
            await NavigateToItemDetail(sender, "Artisan Bread", "Vin Bakery");
        }

        private async void OnPreferredButterCroissantTapped(object sender, EventArgs e)
        {
            await NavigateToItemDetail(sender, "Butter Croissant", "Vin Bakery");
        }

        private async void OnPreferredChocoCakeTapped(object sender, EventArgs e)
        {
            await NavigateToItemDetail(sender, "Choco Cake", "Vin Bakery");
        }

        private async void OnPreferredGlazedDonutTapped(object sender, EventArgs e)
        {
            await NavigateToItemDetail(sender, "Glazed Donut", "Welove Bakery");
        }

        private async void OnPreferredChocoChipTapped(object sender, EventArgs e)
        {
            await NavigateToItemDetail(sender, "Choco Chip", "Welove Bakery");
        }

        private async void OnPreferredBlueberryMuffinTapped(object sender, EventArgs e)
        {
            await NavigateToItemDetail(sender, "Blueberry Muffin", "Welove Bakery");
        }

        private async Task NavigateToItemDetail(object sender, string itemName, string storeName)
        {
            if (string.IsNullOrWhiteSpace(itemName)) itemName = "Selected Item";
            if (string.IsNullOrWhiteSpace(storeName)) storeName = "Unknown Store";

            if (sender is VisualElement element)
            {
                await element.ScaleTo(0.95, 80, Easing.CubicOut);
                await element.ScaleTo(1, 80, Easing.CubicIn);
            }

            await Navigation.PushAsync(new ItemDetailPage(itemName, storeName));
        }

        private async void OnFreeDeliveryTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Free Surplus Food", "Enjoy your food everyday!", "OK");
        }

        private async void OnSpecialOfferingTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Special for Everyone", "Use M4Food for order free food.", "OK");
        }

        private async void OnSeeAllTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Preferred", "Showing all your preferred items...", "OK");
        }

        private async void OnHomeTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Home", "You are on the Home screen.", "OK");
        }

        private async void OnCartTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CartPage());
        }

        private async void OnAccountTapped(object sender, EventArgs e)
        {
            // 1. Display the Pop-up (ActionSheet) with the new options
            string action = await DisplayActionSheet(
                "Account Options",
                "Cancel",
                null,
                "Edit Profile",
                "Sign Up for Donation"); // Changed from "Sign Up / Log In" to "Sign Up for Donation"

            // 2. Handle the User's Choice
            if (action == "Edit Profile")
            {
                // Navigate to Profile Page
                await Navigation.PushAsync(new M4Food.Views.ProfilePage());
            }
            else if (action == "Sign Up for Donation")
            {
                // Navigate to the new Donation Page
                await Navigation.PushAsync(new M4Food.Views.DonationPage());
            }
        }

        private async void OnOrdersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OrdersPage());
        }
    }
}