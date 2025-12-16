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

            // FIX 1: 图片加载逻辑 (假设 ItemImage 存在于 XAML)
            string sanitizedItemName = _itemName.Replace(" ", "").ToLower();
            // 警告: Image.Source 属性在某些旧平台上不受支持，但功能上是正确的。
            ItemImage.Source = $"{sanitizedItemName}.png";
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            // 警告: Navigation.PopAsync() 在某些旧平台上不受支持，但功能上是正确的。
            await Navigation.PopAsync();
        }

        // FIX 2: 增加库存限制逻辑和 await Task.CompletedTask
        private async void OnQuantityIncreased(object sender, EventArgs e)
        {
            if (_quantity < _availableStock)
            {
                _quantity++;
                // 警告: Label.Text 属性在某些旧平台上不受支持，但功能上是正确的。
                QuantityLabel.Text = _quantity.ToString();

                // FIX: 增加 await Task.CompletedTask 解决 async warning
                await Task.CompletedTask;
            }
            else
            {
                // FIX: 增加库存上限提示
                // 警告: DisplayAlert 在某些旧平台上不受支持，但功能上是正确的。
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