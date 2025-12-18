using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace M4Food.Views
{
    public partial class OrdersPage : ContentPage
    {
        private bool _showActiveOrders = true;
        private List<Order> _allOrders = new List<Order>();

        public OrdersPage()
        {
            InitializeComponent();
            LoadSampleOrders();
            ShowActiveOrders();
        }

        private void LoadSampleOrders()
        {
            _allOrders = new List<Order>
            {
                new Order
                {
                    OrderId = "ORD001",
                    OrderDate = DateTime.Now.AddHours(-2),
                    Status = "Processing",
                    Items = new ObservableCollection<OrderItem>
                    {
                        new OrderItem { Name = "Artisan Bread", Quantity = 2, Price = 3.50 },
                        new OrderItem { Name = "Butter Croissant", Quantity = 1, Price = 2.80 }
                    }
                },
                new Order
                {
                    OrderId = "ORD002",
                    OrderDate = DateTime.Now.AddDays(-1),
                    Status = "Processing",
                    Items = new ObservableCollection<OrderItem>
                    {
                        new OrderItem { Name = "Choco Cake", Quantity = 1, Price = 12.90 }
                    }
                },
                new Order
                {
                    OrderId = "ORD003",
                    OrderDate = DateTime.Now.AddDays(-3),
                    Status = "Delivered",
                    Items = new ObservableCollection<OrderItem>
                    {
                        new OrderItem { Name = "Glazed Donut", Quantity = 3, Price = 2.50 },
                        new OrderItem { Name = "Blueberry Muffin", Quantity = 2, Price = 3.20 }
                    }
                },
                new Order
                {
                    OrderId = "ORD004",
                    OrderDate = DateTime.Now.AddDays(-7),
                    Status = "Cancelled",
                    Items = new ObservableCollection<OrderItem>
                    {
                        new OrderItem { Name = "Choco Chip", Quantity = 4, Price = 1.80 }
                    }
                },
                new Order
                {
                    OrderId = "ORD005",
                    OrderDate = DateTime.Now.AddDays(-10),
                    Status = "Delivered",
                    Items = new ObservableCollection<OrderItem>
                    {
                        new OrderItem { Name = "Artisan Bread", Quantity = 1, Price = 3.50 },
                        new OrderItem { Name = "Butter Croissant", Quantity = 2, Price = 2.80 },
                        new OrderItem { Name = "Choco Cake", Quantity = 1, Price = 12.90 }
                    }
                }
            };

            foreach (var order in _allOrders)
            {
                order.TotalPrice = order.Items.Sum(item => item.Quantity * item.Price);
            }
        }

        public void AddNewOrder(Order newOrder)
        {
            newOrder.TotalPrice = newOrder.Items.Sum(item => item.Quantity * item.Price);
            _allOrders.Insert(0, newOrder);

            if (_showActiveOrders)
            {
                ShowActiveOrders();
            }
            else
            {
                ShowPastOrders();
            }
        }

        private void ShowActiveOrders()
        {
            _showActiveOrders = true;
            ActiveIndicator.IsVisible = true;
            PastIndicator.IsVisible = false;

            ActiveOrdersLabel.TextColor = Color.FromArgb("#1A1A1A");
            PastOrdersLabel.TextColor = Color.FromArgb("#999999");

            var activeOrders = _allOrders
                .Where(o => o.Status == "Processing")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (activeOrders.Count == 0)
            {
                OrdersCollectionView.ItemsSource = new List<Order>();
                ShowEmptyState("No Order Found");
            }
            else
            {
                OrdersCollectionView.ItemsSource = activeOrders;
                OrdersCollectionView.EmptyView = null;
            }
        }

        private void ShowPastOrders()
        {
            _showActiveOrders = false;
            ActiveIndicator.IsVisible = false;
            PastIndicator.IsVisible = true;

            ActiveOrdersLabel.TextColor = Color.FromArgb("#999999");
            PastOrdersLabel.TextColor = Color.FromArgb("#1A1A1A");

            var pastOrders = _allOrders
                .Where(o => o.Status == "Delivered" || o.Status == "Cancelled")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (pastOrders.Count == 0)
            {
                OrdersCollectionView.ItemsSource = new List<Order>();
                ShowEmptyState("No Order Found");
            }
            else
            {
                OrdersCollectionView.ItemsSource = pastOrders;
                OrdersCollectionView.EmptyView = null;
            }
        }

        private void ShowEmptyState(string message)
        {
            var emptyView = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 20,
                Margin = new Thickness(0, 50, 0, 0)
            };

            emptyView.Children.Add(new Label
            {
                Text = "📋",
                FontSize = 60,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb("#CCCCCC")
            });

            emptyView.Children.Add(new Label
            {
                Text = message,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#999999"),
                HorizontalOptions = LayoutOptions.Center
            });


            OrdersCollectionView.EmptyView = emptyView;
        }

        private void OnActiveOrdersTapped(object sender, EventArgs e)
        {
            ShowActiveOrders();
        }

        private void OnPastOrdersTapped(object sender, EventArgs e)
        {
            ShowPastOrders();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }

    public class OrderItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
    }

    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public ObservableCollection<OrderItem> Items { get; set; } = new ObservableCollection<OrderItem>();
        public double TotalPrice { get; set; }

        public Color StatusColor
        {
            get
            {
                return Status switch
                {
                    "Processing" => Color.FromArgb("#FFA500"),
                    "Delivered" => Color.FromArgb("#4CAF50"),
                    "Cancelled" => Color.FromArgb("#FF4B4B"),
                    _ => Color.FromArgb("#666666")
                };
            }
        }

        public string ItemsSummary
        {
            get
            {
                if (Items == null || Items.Count == 0)
                    return "No items";

                var itemCount = Items.Sum(i => i.Quantity);
                var firstTwoItems = Items.Take(2);
                var itemNames = string.Join(", ", firstTwoItems.Select(i => $"{i.Quantity}x {i.Name}"));

                var result = $"{itemCount} item{(itemCount > 1 ? "s" : "")}: {itemNames}";

                if (Items.Count > 2)
                    result += "...";

                return result;
            }
        }
    }
}