using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;

namespace M4Food.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        private readonly string _itemName;
        private double _itemPrice = 5.00;
        private int _availableStock = 10;
        private int _quantity = 1;

        public ItemDetailPage(string itemName)
        {
            InitializeComponent();

            _itemName = string.IsNullOrWhiteSpace(itemName) ? "Unknown Item" : itemName;
            this.Title = _itemName;

            if (QuantityLabel != null)
            {
                QuantityLabel.Text = _quantity.ToString();
            }
        }

        /// <summary>
        /// Check if the device has internet connection
        /// </summary>
        private bool IsConnected()
        {
            var networkAccess = Connectivity.Current.NetworkAccess;
            return networkAccess == NetworkAccess.Internet;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadItemDetails();
        }

        private void LoadItemDetails()
        {
            try
            {
                if (NameLabel != null)
                {
                    NameLabel.Text = _itemName;
                }
                
                if (StockLabel != null)
                {
                    StockLabel.Text = $"Stock: {_availableStock} available";
                }

                string description = _itemName switch
                {
                    "Artisan Bread" => "Freshly baked artisan bread, perfect for toast and sandwiches.",
                    "Butter Croissant" => "Flaky, buttery croissant, a classic breakfast delight.",
                    "Choco Cake" => "Rich chocolate cake, guaranteed to satisfy your sweet cravings.",
                    "Glazed Donut" => "Classic glazed ring donut, soft and sweet.",
                    "Choco Chip" => "Chewy chocolate chip cookie, a timeless favorite.",
                    "Blueberry Muffin" => "Moist blueberry muffin, baked fresh every morning.",
                    _ => "A wonderful, freshly baked item waiting for a good home."
                };

                if (DescriptionLabel != null)
                {
                    DescriptionLabel.Text = description;
                }

                if (ItemImage != null)
                {
                    string sanitizedItemName = _itemName?.Replace(" ", "").ToLower() ?? "unknown";
                    ItemImage.Source = $"{sanitizedItemName}.png";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading item details: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnQuantityIncreased(object sender, EventArgs e)
        {
            if (_quantity < _availableStock)
            {
                _quantity++;
                QuantityLabel.Text = _quantity.ToString();
                await Task.CompletedTask;
            }
            else
            {
                await DisplayAlert("Out of Stock", $"Maximum quantity reached. Current stock: {_availableStock}.", "OK");
            }
        }

        private async void OnQuantityDecreased(object sender, EventArgs e)
        {
            if (_quantity > 1)
            {
                _quantity--;
                QuantityLabel.Text = _quantity.ToString();
            }
            await Task.CompletedTask;
        }

        private async void OnAddToCartClicked(object sender, EventArgs e)
        {
            // Check if device is online
            if (!IsConnected())
            {
                await DisplayAlert(
                    "Offline", 
                    "You are currently offline. Please connect to the internet to add items to your cart.", 
                    "OK");
                return;
            }

            for (int i = 0; i < _quantity; i++)
            {
                M4Food.Views.CartService.Current.AddOrUpdateItem(_itemName);
            }

            await DisplayAlert("Added to Cart", $"{_quantity} x {_itemName} added to your cart!", "OK");
            await Navigation.PopAsync();
        }
    }
}