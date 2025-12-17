using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace M4Food.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        private readonly string _itemName;
        private double _itemPrice = 5.00;
        private int _availableStock = 10;
        private int _quantity = 1;

        // Constructor: Receives the item name passed from the Main Page
        public ItemDetailPage(string itemName)
        {
            InitializeComponent();

            _itemName = string.IsNullOrWhiteSpace(itemName) ? "Unknown Item" : itemName;
            this.Title = _itemName;

            // Defer heavy operations to OnAppearing to improve initial load performance
            // Set initial quantity display (with null check)
            if (QuantityLabel != null)
            {
                QuantityLabel.Text = _quantity.ToString();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Load item details after page appears for better perceived performance
            LoadItemDetails();
        }

        private void LoadItemDetails()
        {
            try
            {
                // Set item name, price, and stock on the UI elements (with null checks)
                if (NameLabel != null)
                {
                    NameLabel.Text = _itemName;
                }
                if (PriceLabel != null)
                {
                    PriceLabel.Text = $"RM {_itemPrice:F2}";
                }
                if (StockLabel != null)
                {
                    StockLabel.Text = $"Stock: {_availableStock} available";
                }

                // Simulate description based on item name
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

                // Image loading logic (assumes ItemImage exists in XAML)
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
            // Note: Navigation.PopAsync() might not be supported on some legacy platforms, but is functionally correct.
            await Navigation.PopAsync();
        }

        // Increase quantity with stock limit and await Task.CompletedTask
        private async void OnQuantityIncreased(object sender, EventArgs e)
        {
            if (_quantity < _availableStock)
            {
                _quantity++;
                // Note: Label.Text might not be supported on some legacy platforms, but is functionally correct.
                QuantityLabel.Text = _quantity.ToString();

                // Use await Task.CompletedTask to satisfy async signature
                await Task.CompletedTask;
            }
            else
            {
                // Show max stock warning
                // Note: DisplayAlert might not be supported on some legacy platforms, but is functionally correct.
                await DisplayAlert("Out of Stock", $"Maximum quantity reached. Current stock: {_availableStock}.", "OK");
            }
        }

        // FIX 2: 增加 await Task.CompletedTask
        private async void OnQuantityDecreased(object sender, EventArgs e)
        {
            if (_quantity > 1)
            {
                _quantity--;
                // 警告: Label.Text 属性在某些旧平台上不受支持，但功能上是正确的。
                QuantityLabel.Text = _quantity.ToString();
            }
            // FIX: 增加 await Task.CompletedTask 解决 async warning
            await Task.CompletedTask;
        }

        private async void OnAddToCartClicked(object sender, EventArgs e)
        {
            // Add the selected quantity to the Cart Service
            for (int i = 0; i < _quantity; i++)
            {
                // 假设 CartService 位于 M4Food.Views 命名空间
                M4Food.Views.CartService.Current.AddOrUpdateItem(_itemName);
            }

            // Provide feedback and navigate back
            await DisplayAlert("Added to Cart", $"{_quantity} x {_itemName} added to your cart!", "OK");
            await Navigation.PopAsync(); // Navigate back to the previous page (Main Page)
        }
    }
}