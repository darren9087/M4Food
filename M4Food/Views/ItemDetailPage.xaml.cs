using System;
using System.Xml;
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

            _itemName = itemName;
            this.Title = itemName;

            // Simulate data loading
            LoadItemDetails();

            // Set initial quantity display
            QuantityLabel.Text = _quantity.ToString();
        }

        private void LoadItemDetails()
        {
            // Set item name, price, and stock on the UI elements
            NameLabel.Text = _itemName;
            PriceLabel.Text = $"RM {_itemPrice:F2}";
            StockLabel.Text = $"Stock: {_availableStock} available";

            // Simulate description based on item name
            DescriptionLabel.Text = _itemName switch
            {
                "Artisan Bread" => "Freshly baked artisan bread, perfect for toast and sandwiches.",
                "Butter Croissant" => "Flaky, buttery croissant, a classic breakfast delight.",
                "Choco Cake" => "Rich chocolate cake, guaranteed to satisfy your sweet cravings.",
                "Glazed Donut" => "Classic glazed ring donut, soft and sweet.",
                "Choco Chip" => "Chewy chocolate chip cookie, a timeless favorite.",
                "Blueberry Muffin" => "Moist blueberry muffin, baked fresh every morning.",
                _ => "A wonderful, freshly baked item waiting for a good home."
            };
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnQuantityIncreased(object sender, EventArgs e)
        {
            if (_quantity < _availableStock)
            {
                _quantity++;
                QuantityLabel.Text = _quantity.ToString();
            }
        }

        private void OnQuantityDecreased(object sender, EventArgs e)
        {
            if (_quantity > 1)
            {
                _quantity--;
                QuantityLabel.Text = _quantity.ToString();
            }
        }

        private async void OnAddToCartClicked(object sender, EventArgs e)
        {
            // Add the selected quantity to the Cart Service
            for (int i = 0; i < _quantity; i++)
            {
                // Assuming CartService is globally accessible and exists
                // Note: The CartService class is not defined in this file.
                // It's called here based on the original code logic.
                // If it doesn't exist, this line will cause a compilation error.
                CartService.Current.AddOrUpdateItem(_itemName);
            }

            // Provide feedback and navigate back
            await DisplayAlert("Added to Cart", $"{_quantity} x {_itemName} added to your cart!", "OK");
            await Navigation.PopAsync(); // Navigate back to the previous page (Main Page)
        }
    }
}