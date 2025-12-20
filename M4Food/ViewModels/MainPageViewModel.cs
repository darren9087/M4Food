using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using Microsoft.Maui.Controls;
using M4Food.Views;
using M4Food.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;


namespace M4Food.ViewModels
{
    /// <summary>
    /// ViewModel for MainPage (Dashboard) - Implements MVVM pattern
    /// E2 Requirement: MVVM-Driven Dashboard with Data Binding and App Lifecycle
    /// </summary>
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private string _selectedCategory = "All";
        private string _categoryResultsTitle = "Category Products (Selected: All)";
        private bool _isLoading = false;
        private int _cartItemCount = 0;
        private bool _isCartBadgeVisible = false;
        private int _activeOrdersCount = 0;
        private bool _isOrdersBadgeVisible = false;

        public MainPageViewModel()
        {
            // Initialize commands
            CategorySelectedCommand = new Command<string>(OnCategorySelected);
            ProductTappedCommand = new Command<ProductItemViewModel>(OnProductTapped);
            CartTappedCommand = new Command(OnCartTapped);
            OrdersTappedCommand = new Command(OnOrdersTapped);
            AccountTappedCommand = new Command(OnAccountTapped);
            HomeTappedCommand = new Command(OnHomeTapped);
            MapTappedCommand = new Command(OnMapTapped);
            FreeDeliveryTappedCommand = new Command(OnFreeDeliveryTapped);
            SpecialOfferingTappedCommand = new Command(OnSpecialOfferingTapped);
            SeeAllTappedCommand = new Command(OnSeeAllTapped);

            // Initialize products
            InitializeProducts();

            // Subscribe to cart changes
            CartService.Current.CartChanged += OnCartChanged;
            UpdateCartBadge();
            
            // Load active orders count
            _ = LoadActiveOrdersCountAsync();
        }

        #region Properties

        public ObservableCollection<ProductItemViewModel> AllProducts { get; } = new ObservableCollection<ProductItemViewModel>();
        public ObservableCollection<ProductItemViewModel> FilteredProducts { get; } = new ObservableCollection<ProductItemViewModel>();

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    FilterProductsByCategory();
                }
            }
        }

        public string CategoryResultsTitle
        {
            get => _categoryResultsTitle;
            set
            {
                if (_categoryResultsTitle != value)
                {
                    _categoryResultsTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CartItemCount
        {
            get => _cartItemCount;
            set
            {
                if (_cartItemCount != value)
                {
                    _cartItemCount = value;
                    OnPropertyChanged();
                    IsCartBadgeVisible = value > 0;
                }
            }
        }

        public bool IsCartBadgeVisible
        {
            get => _isCartBadgeVisible;
            set
            {
                if (_isCartBadgeVisible != value)
                {
                    _isCartBadgeVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CartBadgeText => CartItemCount > 99 ? "99+" : CartItemCount.ToString();

        public int ActiveOrdersCount
        {
            get => _activeOrdersCount;
            set
            {
                if (_activeOrdersCount != value)
                {
                    _activeOrdersCount = value;
                    OnPropertyChanged();
                    IsOrdersBadgeVisible = value > 0;
                }
            }
        }

        public bool IsOrdersBadgeVisible
        {
            get => _isOrdersBadgeVisible;
            set
            {
                if (_isOrdersBadgeVisible != value)
                {
                    _isOrdersBadgeVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OrdersBadgeText => ActiveOrdersCount > 99 ? "99+" : ActiveOrdersCount.ToString();

        #endregion

        #region Commands

        public ICommand CategorySelectedCommand { get; }
        public ICommand ProductTappedCommand { get; }
        public ICommand CartTappedCommand { get; }
        public ICommand OrdersTappedCommand { get; }
        public ICommand AccountTappedCommand { get; }
        public ICommand HomeTappedCommand { get; }
        public ICommand MapTappedCommand { get; }
        public ICommand FreeDeliveryTappedCommand { get; }
        public ICommand SpecialOfferingTappedCommand { get; }
        public ICommand SeeAllTappedCommand { get; }

        #endregion

        #region Private Methods

        private void InitializeProducts()
        {
            var products = new[]
            {
                new ProductItemViewModel { Name = "Artisan Bread", Category = "Bread", ImageSource = "artisanbread.png", StoreName = "Vin Bakery" },
                new ProductItemViewModel { Name = "Butter Croissant", Category = "Bread", ImageSource = "buttercroissant.png", StoreName = "Vin Bakery" },
                new ProductItemViewModel { Name = "Choco Cake", Category = "Cake", ImageSource = "chococake.png", StoreName = "Vin Bakery" },
                new ProductItemViewModel { Name = "Blueberry Muffin", Category = "Cake", ImageSource = "muffin.png", StoreName = "Welove Bakery" },
                new ProductItemViewModel { Name = "Glazed Donut", Category = "Others", ImageSource = "glazeddonut.png", StoreName = "Welove Bakery" },
                new ProductItemViewModel { Name = "Choco Chip", Category = "Others", ImageSource = "chocochip.png", StoreName = "Welove Bakery" },
            };

            foreach (var product in products)
            {
                AllProducts.Add(product);
            }

            FilterProductsByCategory();
        }

        private void FilterProductsByCategory()
        {
            FilteredProducts.Clear();
            
            var filtered = SelectedCategory == "All"
                ? AllProducts
                : AllProducts.Where(p => p.Category == SelectedCategory);

            foreach (var product in filtered)
            {
                FilteredProducts.Add(product);
            }

            CategoryResultsTitle = $"Category Products (Selected: {SelectedCategory})";
        }

        private void OnCategorySelected(string category)
        {
            SelectedCategory = category;
        }

        private async void OnProductTapped(ProductItemViewModel product)
        {
            if (product == null) return;

            // Navigate to item detail page
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation != null)
            {
                await navigation.PushAsync(new Views.ItemDetailPage(product.Name ?? "Unknown Item", product.StoreName ?? "Unknown Store"));
            }
        }

        private async void OnCartTapped()
        {
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation != null)
            {
                await navigation.PushAsync(new Views.CartPage());
            }
        }

        private async void OnOrdersTapped()
        {
            var navigation = Application.Current?.MainPage?.Navigation;
            if (navigation != null)
            {
                await navigation.PushAsync(new Views.OrdersPage());
            }
        }

        private async void OnAccountTapped()
        {
            var page = Application.Current?.MainPage;
            if (page != null)
            {
                string action = await page.DisplayActionSheet(
                    "Account Options",
                    "Cancel",
                    null,
                    "Edit Profile",
                    "Sign Up for Donation");

                if (action == "Edit Profile")
                {
                    await page.Navigation.PushAsync(new Views.ProfilePage());
                }
                else if (action == "Sign Up for Donation")
                {
                    await page.Navigation.PushAsync(new Views.DonationPage());
                }
            }
        }

        private async void OnHomeTapped()
        {
            var page = Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("Home", "You are on the Home screen.", "OK");
            }
        }

        private async void OnMapTapped()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location == null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Location Error",
                        "Unable to retrieve your current location.",
                        "OK");
                    return;
                }

                double latitude = location.Latitude;
                double longitude = location.Longitude;

                string googleMapsUrl =
                    $"https://www.google.com/maps/search/?api=1&query={latitude},{longitude}";

                await Launcher.Default.OpenAsync(googleMapsUrl);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
        }


        private async void OnFreeDeliveryTapped()
        {
            var page = Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("Free Surplus Food", "Enjoy your food everyday!", "OK");
            }
        }

        private async void OnSpecialOfferingTapped()
        {
            var page = Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("Special for Everyone", "Use M4Food for order free food.", "OK");
            }
        }

        private async void OnSeeAllTapped()
        {
            var page = Application.Current?.MainPage;
            if (page != null)
            {
                await page.DisplayAlert("Preferred", "Showing all your preferred items...", "OK");
            }
        }

        private void OnCartChanged(object? sender, EventArgs e)
        {
            UpdateCartBadge();
        }

        private void UpdateCartBadge()
        {
            CartItemCount = CartService.Current.TotalItemCount;
            OnPropertyChanged(nameof(CartBadgeText));
        }

        public async Task LoadActiveOrdersCountAsync()
        {
            try
            {
                var orderService = Application.Current?.Handler?.MauiContext?.Services
                    .GetService(typeof(IOrderService)) as IOrderService;
                
                if (orderService != null)
                {
                    var orders = await orderService.GetOrdersAsync();
                    // Count active orders (Processing, Pending - same logic as OrderHistory page)
                    var activeCount = orders.Count(o => 
                        o.Status == "Processing" || o.Status == "Pending");
                    
                    ActiveOrdersCount = activeCount;
                    OnPropertyChanged(nameof(OrdersBadgeText));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active orders count: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Product item view model for data binding
    /// </summary>
    public class ProductItemViewModel : INotifyPropertyChanged
    {
        private string? _name;
        private string? _category;
        private string? _imageSource;
        private string? _storeName;

        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? ImageSource
        {
            get => _imageSource;
            set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? StoreName
        {
            get => _storeName;
            set
            {
                if (_storeName != value)
                {
                    _storeName = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

