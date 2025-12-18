using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace M4Food.Views
{
    public partial class MainPage : ContentPage
    {
        private class Product
        {
            public string? Name { get; set; }
            public string? Category { get; set; }
            public string? ImageSource { get; set; }
            public string? StoreName { get; set; }
        }

        private List<Product> _allProducts = new List<Product>
        {
            new Product { Name = "Artisan Bread", Category = "Bread", ImageSource = "artisanbread.png", StoreName = "Vin Bakery" },
            new Product { Name = "Butter Croissant", Category = "Bread", ImageSource = "buttercroissant.png", StoreName = "Vin Bakery" },
            new Product { Name = "Choco Cake", Category = "Cake", ImageSource = "chococake.png", StoreName = "Vin Bakery" },
            new Product { Name = "Blueberry Muffin", Category = "Cake", ImageSource = "muffin.png", StoreName = "Welove Bakery" },
            new Product { Name = "Glazed Donut", Category = "Others", ImageSource = "glazeddonut.png", StoreName = "Welove Bakery" },
            new Product { Name = "Choco Chip", Category = "Others", ImageSource = "chocochip.png", StoreName = "Welove Bakery" },
        };

        public MainPage()
        {
            InitializeComponent();
            PopulateCategoryResults("All");
            UpdateCategoryButtonStyle("All");

            // Subscribe to cart changes to update badge
            CartService.Current.CartChanged += OnCartChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Update cart badge when page appears
            UpdateCartBadge();

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
#endif
        }

        private void OnCartChanged(object? sender, EventArgs e)
        {
            // Update badge on main thread
            MainThread.BeginInvokeOnMainThread(UpdateCartBadge);
        }

        private void UpdateCartBadge()
        {
            var count = CartService.Current.TotalItemCount;
            
            if (count > 0)
            {
                CartBadge.IsVisible = true;
                CartBadgeLabel.Text = count > 99 ? "99+" : count.ToString();
            }
            else
            {
                CartBadge.IsVisible = false;
            }
        }

        private void PopulateCategoryResults(string category)
        {
            IEnumerable<Product> filteredProducts = _allProducts;
            if (category != "All")
            {
                filteredProducts = _allProducts.Where(p => p.Category == category);
            }

            CategoryResultsTitle.Text = $"Category Products (Selected: {category})";
            CategoryResultsGrid.Children.Clear();

            int col = 0;
            int row = 0;

            CategoryResultsGrid.RowDefinitions.Clear();
            int requiredRows = (int)Math.Ceiling((double)filteredProducts.Count() / 3);
            for (int i = 0; i < requiredRows; i++)
            {
                CategoryResultsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            foreach (var product in filteredProducts)
            {
                var stackLayout = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Spacing = 5,
                    InputTransparent = true
                };

                var productImage = new Image
                {
                    Source = product.ImageSource,
                    Aspect = Aspect.AspectFit,
                    HeightRequest = 50,
                    WidthRequest = 50,
                    InputTransparent = true
                };

                var imageFrame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#EEEEEE"),
                    CornerRadius = 10,
                    HeightRequest = 50,
                    WidthRequest = 50,
                    HasShadow = false,
                    HorizontalOptions = LayoutOptions.Center,
                    Padding = new Thickness(0),
                    InputTransparent = true,
                    Content = productImage
                };
                stackLayout.Children.Add(imageFrame);

                var nameLabel = new Label
                {
                    Text = product.Name,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1A1A1A"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    InputTransparent = true
                };
                stackLayout.Children.Add(nameLabel);

                var outerFrame = new Frame
                {
                    CornerRadius = 15,
                    Padding = new Thickness(10),
                    HasShadow = false,
                    BorderColor = Colors.Transparent,
                    BackgroundColor = Colors.White,
                    MinimumHeightRequest = 100,
                    MinimumWidthRequest = 100,
                    Content = stackLayout
                };

                string productName = product.Name ?? "Unknown Item";
                string productStore = product.StoreName ?? "Unknown Store";

                var tapGesture = new TapGestureRecognizer
                {
                    NumberOfTapsRequired = 1
                };
                tapGesture.Tapped += (s, args) =>
                {
                    _ = NavigateToItemDetail(outerFrame, productName, productStore);
                };
                outerFrame.GestureRecognizers.Add(tapGesture);

                CategoryResultsGrid.Children.Add(outerFrame);
                Microsoft.Maui.Controls.Grid.SetColumn(outerFrame, col);
                Microsoft.Maui.Controls.Grid.SetRow(outerFrame, row);

                col++;
                if (col > 2)
                {
                    col = 0;
                    row++;
                }
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

            string category = "All";
            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                category = param;
            }

            PopulateCategoryResults(category);
            UpdateCategoryButtonStyle(category);
            await ScrollViewContainer.ScrollToAsync(CategoryResultsSection, ScrollToPosition.Start, true);
        }

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
            await DisplayAlert("Free Delivery", "Enjoy your food everyday!", "OK");
        }

        private async void OnSpecialOfferingTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Special for Everyone", "Welcome to use M4Food for order free food.", "OK");
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
            await Navigation.PushAsync(new M4Food.Views.ProfilePage());
        }

        private async void OnOrdersTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OrdersPage());
        }
    }
}