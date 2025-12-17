using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace M4Food.Views
{
    // ----------------------------------------------------------------------
    // CartItem: Item Model, implementing INotifyPropertyChanged
    // ----------------------------------------------------------------------
    public class CartItem : INotifyPropertyChanged
    {
        private int _quantity = 1;

        public string? Name { get; set; }
        public double Price { get; set; } = 5.00; // Assumed price

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPrice)); // Notify that TotalPrice has changed
                }
            }
        }

        // Calculated property for the total price of this item
        public double TotalPrice => Price * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ----------------------------------------------------------------------
    // CartService: Shopping Cart Management Service (Singleton pattern, public constructor)
    // ----------------------------------------------------------------------
    public class CartService
    {
        private static CartService? _instance;

        // Public static accessor to get the unique instance (Lazy initialization)
        public static CartService Current => _instance ??= new CartService();

        // Must be public to avoid XAML runtime errors when binding
        public CartService() { }

        // ObservableCollection ensures UI updates when items are added or removed
        public ObservableCollection<CartItem> Items { get; } = new ObservableCollection<CartItem>();

        public void AddOrUpdateItem(string itemName)
        {
            // Check if the item already exists in the cart
            var existingItem = Items.FirstOrDefault(i => i.Name == itemName);

            if (existingItem != null)
            {
                // If exists, increment the quantity
                existingItem.Quantity++;
            }
            else
            {
                // If not, add a new CartItem
                Items.Add(new CartItem { Name = itemName });
            }
        }

        public double GetTotal()
        {
            // Calculate the grand total of all items
            return Items.Sum(i => i.TotalPrice);
        }

        public void ClearCart()
        {
            // Remove all items from the cart
            Items.Clear();
        }
    }
}