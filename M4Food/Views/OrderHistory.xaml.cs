using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using M4Food.Services;
#if ANDROID
using M4Food;
using Microsoft.Maui.ApplicationModel;
#endif
using Microsoft.Maui.Storage;
using System.Net.Http;

namespace M4Food.Views
{
    public partial class OrdersPage : ContentPage
    {
        private bool _showActiveOrders = true;
        private List<Order> _allOrders = new List<Order>();
        private readonly IOrderService _orderService;
        private readonly ICloudinaryService? _cloudinaryService;

        public OrdersPage()
        {
            InitializeComponent();

            // Get OrderService from DI
            _orderService = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService(typeof(IOrderService)) as IOrderService
                ?? throw new InvalidOperationException("IOrderService not registered.");

            // Get CloudinaryService from DI (optional)
            _cloudinaryService = Application.Current?
                .Handler?
                .MauiContext?
                .Services
                .GetService(typeof(ICloudinaryService)) as ICloudinaryService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                // Show loading indicator
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                OrdersCollectionView.IsVisible = false;

                // Load orders from Firebase
                _allOrders = await _orderService.GetOrdersAsync();

                foreach (var order in _allOrders)
                {
                    System.Diagnostics.Debug.WriteLine($"Order {order.OrderId} Store: {order.StoreDisplay}");
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {_allOrders.Count} orders");

                // Refresh the current view
                if (_showActiveOrders)
                {
                    ShowActiveOrders();
                }
                else
                {
                    ShowPastOrders();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex.Message}");
                await DisplayAlert("Error", "Failed to load orders. Please try again.", "OK");
            }
            finally
            {
                // Hide loading indicator
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                OrdersCollectionView.IsVisible = true;
            }
        }

        /// <summary>
        /// Called from CartPage after a new order is placed.
        /// Adds the order to the local list and refreshes the view.
        /// </summary>
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
                .Where(o => o.Status == "Processing" || o.Status == "Pending")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (activeOrders.Count == 0)
            {
                OrdersCollectionView.ItemsSource = new List<Order>();
                ShowEmptyState("No Active Orders");
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
                .Where(o => o.Status == "Delivered" || o.Status == "Cancelled" || o.Status == "Completed")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (pastOrders.Count == 0)
            {
                OrdersCollectionView.ItemsSource = new List<Order>();
                ShowEmptyState("No Past Orders");
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

            emptyView.Children.Add(new Label
            {
                Text = "Your orders will appear here",
                FontSize = 14,
                TextColor = Color.FromArgb("#BBBBBB"),
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

        private async void OnConfirmReceivedClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string orderId)
            {
                var confirm = await DisplayAlert(
                    "Confirm Received",
                    "Have you received your order?",
                    "Yes, Received",
                    "Cancel");

                if (!confirm) return;

                // Ask if user wants to upload a photo (optional)
                string? imageUrl = null;
                var uploadPhoto = await DisplayAlert(
                    "Upload Photo",
                    "Would you like to upload a photo of your received order? (Optional)",
                    "Yes, Upload Photo",
                    "Skip");

                if (uploadPhoto && _cloudinaryService != null)
                {
                    try
                    {
                        // Ask user to choose between camera or gallery
                        var action = await DisplayActionSheet(
                            "Select Photo Source",
                            "Cancel",
                            null,
                            "Take Photo",
                            "Choose from Gallery");

                        if (action == "Take Photo" || action == "Choose from Gallery")
                        {
                            FileResult? photo = null;
                            
                            if (action == "Take Photo")
                            {
                                // Request camera permission on Android
#if ANDROID
                                var status = await Permissions.RequestAsync<Permissions.Camera>();
                                if (status != PermissionStatus.Granted)
                                {
                                    await DisplayAlert("Permission Required", 
                                        "Camera permission is required to take photos. Please enable it in app settings.", 
                                        "OK");
                                    return;
                                }
#endif
                                photo = await MediaPicker.Default.CapturePhotoAsync();
                            }
                            else if (action == "Choose from Gallery")
                            {
                                photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                                {
                                    Title = "Select a photo of your order"
                                });
                            }

                            if (photo != null)
                            {
                                // Show loading indicator
                                await DisplayAlert("Uploading", "Please wait while we upload your photo...", "OK");

                                // Upload to Cloudinary
                                using var stream = await photo.OpenReadAsync();
                                var (url, _) = await _cloudinaryService.UploadImageStreamAsync(
                                    stream,
                                    photo.FileName ?? $"order_{orderId}_{DateTime.UtcNow.Ticks}.jpg",
                                    folder: "m4food/orders/received"
                                );
                                
                                imageUrl = url;
                                System.Diagnostics.Debug.WriteLine($"Image uploaded successfully: {imageUrl}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error uploading photo: {ex.Message}");
                        // Continue without photo if upload fails
                        await DisplayAlert("Photo Upload Failed", 
                            "Could not upload photo. Order will be confirmed without photo.", 
                            "OK");
                    }
                }

                // Download image to local cache for offline access
                string? localImagePath = null;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try
                    {
                        localImagePath = await DownloadImageToLocalCacheAsync(imageUrl, orderId);
                        System.Diagnostics.Debug.WriteLine($"Image cached locally: {localImagePath}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error caching image locally: {ex.Message}");
                        // Continue without local cache if download fails
                    }
                }

                try
                {
                    var success = await _orderService.UpdateOrderStatusAsync(orderId, "Completed", imageUrl, localImagePath);
                    
                    if (success)
                    {
                        // Update local list
                        var order = _allOrders.FirstOrDefault(o => o.OrderId == orderId);
                        if (order != null)
                        {
                            order.Status = "Completed";
                            order.ReceivedImageUrl = imageUrl;
                            order.ReceivedImageLocalPath = localImagePath;
                        }

                        var message = string.IsNullOrEmpty(imageUrl) 
                            ? "Order marked as completed!" 
                            : "Order marked as completed with photo!";
                        await DisplayAlert("Success", message, "OK");
                        
                        // Refresh the view
                        ShowActiveOrders();

#if ANDROID
                        NotificationHelper.ShowNotification(
                            "Order Completed",
                            $"Thank you! Order #{orderId} has been completed."
                        );
#endif
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to update order status.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error confirming order: {ex.Message}");
                    await DisplayAlert("Error", "Failed to update order. Please try again.", "OK");
                }
            }
        }

        /// <summary>
        /// Downloads an image from URL and saves it to local cache for offline access
        /// </summary>
        private async Task<string?> DownloadImageToLocalCacheAsync(string imageUrl, string orderId)
        {
            try
            {
                // Create cache directory for order images
                var cacheDirectory = Path.Combine(FileSystem.AppDataDirectory, "order_images");
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                // Generate local file path
                var fileName = $"order_{orderId}_{DateTime.UtcNow.Ticks}.jpg";
                var localPath = Path.Combine(cacheDirectory, fileName);

                // Download image
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                var imageData = await httpClient.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(localPath, imageData);

                System.Diagnostics.Debug.WriteLine($"Image downloaded to: {localPath}");
                return localPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading image: {ex.Message}");
                return null;
            }
        }

        private async void OnImageTapped(object sender, EventArgs e)
        {
            if (sender is Image image && image.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && 
                tap.CommandParameter is string orderId)
            {
                var order = _allOrders.FirstOrDefault(o => o.OrderId == orderId);
                if (order != null && !string.IsNullOrEmpty(order.ImageSource))
                {
                    // Show full screen image view
                    await DisplayAlert("Order Image", $"Order #{orderId} received image", "OK");
                }
            }
        }

        private async void OnCancelOrderClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string orderId)
            {
                var confirm = await DisplayAlert(
                    "Cancel Collection",
                    "Are you sure you want to cancel this collection?",
                    "Yes, Cancel",
                    "No");

                if (!confirm) return;

                // Show cancel reason selection
                string? cancelReason = await ShowCancelReasonDialogAsync();
                
                if (cancelReason == null)
                {
                    // User cancelled the reason selection
                    return;
                }

                try
                {
                    var success = await _orderService.CancelOrderAsync(orderId, cancelReason);
                    
                    if (success)
                    {
                        // Update local list
                        var order = _allOrders.FirstOrDefault(o => o.OrderId == orderId);
                        if (order != null)
                        {
                            order.Status = "Cancelled";
                            order.CancelReason = cancelReason;
                        }

                        await DisplayAlert("Cancelled", "Collection has been cancelled.", "OK");
                        
                        // Refresh the view
                        ShowActiveOrders();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to cancel collection.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cancelling order: {ex.Message}");
                    await DisplayAlert("Error", "Failed to cancel collection. Please try again.", "OK");
                }
            }
        }

        /// <summary>
        /// Shows a dialog for user to select or enter cancel reason
        /// </summary>
        private async Task<string?> ShowCancelReasonDialogAsync()
        {
            // Predefined cancel reasons for collectors
            var reasons = new[]
            {
                "Changed my mind, don't need it anymore",
                "Temporary issue, cannot go to collect",
                "Address too far / inconvenient to go",
                "Time not suitable",
                "Found other alternative",
                "Other reason"
            };

            var selectedReason = await DisplayActionSheet(
                "Please select a reason for cancellation:",
                "Cancel",
                null,
                reasons
            );

            if (selectedReason == null || selectedReason == "Cancel")
                return null;

            // If user selected "Other reason", ask them to enter custom reason
            if (selectedReason == "Other reason")
            {
                var customReason = await DisplayPromptAsync(
                    "Other Reason",
                    "Please tell us why you are cancelling:",
                    "OK",
                    "Cancel",
                    "Enter your reason here...",
                    -1,
                    Keyboard.Default
                );

                if (string.IsNullOrWhiteSpace(customReason))
                    return null;

                return $"Other: {customReason}";
            }

            return selectedReason;
        }
    }

    public class OrderItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string StoreName { get; set; } = string.Empty;
    }

    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public ObservableCollection<OrderItem> Items { get; set; } = new ObservableCollection<OrderItem>();
        public double TotalPrice { get; set; }
        public string? ReceivedImageUrl { get; set; }
        public string? ReceivedImageLocalPath { get; set; }
        public string? CancelReason { get; set; }
        
        /// <summary>
        /// Returns the image source path - prefers local path for offline access, falls back to URL
        /// </summary>
        public string? ImageSource
        {
            get
            {
                if (!string.IsNullOrEmpty(ReceivedImageLocalPath) && File.Exists(ReceivedImageLocalPath))
                    return ReceivedImageLocalPath;
                return ReceivedImageUrl;
            }
        }

        public string StoreDisplay
        {
            get
            {
             
                if (Items == null || Items.Count == 0)
                {
                    return "No Store Info";
                }
                return Items[0].StoreName;
            }
        }

        /// <summary>
        /// Returns true if the order is still active (can be confirmed or cancelled)
        /// </summary>
        public bool IsActive => Status == "Processing" || Status == "Pending";

        public Color StatusColor
        {
            get
            {
                return Status switch
                {
                    "Processing" or "Pending" => Color.FromArgb("#FFA500"),
                    "Delivered" or "Completed" => Color.FromArgb("#4CAF50"),
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
