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
        // Product 数据模型
        private class Product
        {
            public string? Name { get; set; }
            public string? Category { get; set; }
            public string? ImageSource { get; set; }
        }

        // 最终修正的数据源：只包含您要求的 6 个产品，并根据您的分类逻辑设置 Category
        private List<Product> _allProducts = new List<Product>
        {
            // --- Bread Category (2 items) ---
            new Product { Name = "Artisan Bread", Category = "Bread", ImageSource = "artisanbread.png" },
            new Product { Name = "Butter Croissant", Category = "Bread", ImageSource = "buttercroissant.png" },

            // --- Cake Category (2 items) ---
            new Product { Name = "Choco Cake", Category = "Cake", ImageSource = "chococake.png" },
            new Product { Name = "Blueberry Muffin", Category = "Cake", ImageSource = "muffin.png" }, // 归类到 Cake

            // --- Others Category (2 items) ---
            new Product { Name = "Glazed Donut", Category = "Others", ImageSource = "glazeddonut.png" },
            new Product { Name = "Choco Chip", Category = "Others", ImageSource = "chocochip.png" },
        };

        public MainPage()
        {
            InitializeComponent();

            // 页面初始化时，默认显示所有 6 个产品
            PopulateCategoryResults("All");
            // 确保默认选中 'All' 按钮的样式
            UpdateCategoryButtonStyle("All");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

#if ANDROID
            // Ensure we have an FCM token even when the app auto-skips the LoginPage
            try
            {
                var cloudMessaging = Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current;
                await cloudMessaging.CheckIfValidAsync();
                var token = await cloudMessaging.GetTokenAsync();
                System.Diagnostics.Debug.WriteLine($"FCM token (MainPage): {token}");

                // Ensure we are subscribed to the daily topic (idempotent)
                await cloudMessaging.SubscribeToTopicAsync("daily");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to init FCM on MainPage: {ex.Message}");
            }
#endif
        }

        // 动态生成产品列表 UI 的方法
        private void PopulateCategoryResults(string category)
        {
            IEnumerable<Product> filteredProducts = _allProducts;
            if (category != "All")
            {
                // 筛选出符合当前分类的产品
                filteredProducts = _allProducts.Where(p => p.Category == category);
            }

            // 更新标题
            CategoryResultsTitle.Text = $"Category Products (Selected: {category})";
            CategoryResultsGrid.Children.Clear();

            // 填充 Grid (三列布局)
            int col = 0;
            int row = 0;

            // 检查并设置 Grid 的 RowDefinitions
            CategoryResultsGrid.RowDefinitions.Clear();
            int requiredRows = (int)Math.Ceiling((double)filteredProducts.Count() / 3);
            for (int i = 0; i < requiredRows; i++)
            {
                CategoryResultsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }


            foreach (var product in filteredProducts)
            {
                // 创建 StackLayout 包含 Image 和 Label
                var stackLayout = new StackLayout 
                { 
                    VerticalOptions = LayoutOptions.Center, 
                    Spacing = 5,
                    InputTransparent = true  // 让点击穿透到外层 Frame
                };

                // Image Frame (MAUI handles image caching automatically)
                var productImage = new Image 
                { 
                    Source = product.ImageSource, 
                    Aspect = Aspect.AspectFit, 
                    HeightRequest = 50, 
                    WidthRequest = 50,
                    InputTransparent = true  // 让点击穿透
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
                    InputTransparent = true,  // 让点击穿透
                    Content = productImage
                };
                stackLayout.Children.Add(imageFrame);

                // Label
                var nameLabel = new Label
                {
                    Text = product.Name,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1A1A1A"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    InputTransparent = true  // 让点击穿透
                };
                stackLayout.Children.Add(nameLabel);

                // 外层可点击 Frame
                var outerFrame = new Frame
                {
                    CornerRadius = 15,
                    Padding = new Thickness(10),
                    HasShadow = false,
                    BorderColor = Colors.Transparent,
                    BackgroundColor = Colors.White,
                    MinimumHeightRequest = 100,  // 确保有足够的点击区域
                    MinimumWidthRequest = 100,
                    Content = stackLayout
                };

                // 添加点击手势 (导航到 ItemDetailPage)
                // 使用闭包捕获 product.Name，确保参数正确传递
                string productName = product.Name ?? "Unknown Item";
                
                // 创建更可靠的手势识别器
                var tapGesture = new TapGestureRecognizer
                {
                    NumberOfTapsRequired = 1
                };
                tapGesture.Tapped += (s, args) => 
                {
                    // 立即响应，不等待
                    _ = NavigateToItemDetail(outerFrame, productName);
                };
                outerFrame.GestureRecognizers.Add(tapGesture);

                // 添加到 Grid
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

        // 辅助方法：更新分类按钮的样式（高亮选中项）
        private void UpdateCategoryButtonStyle(string selectedCategory)
        {
            // 定义颜色
            var selectedColor = Color.FromHex("#EBC656");
            var defaultTextColor = Color.FromHex("#EBC656");
            var defaultBgColor = Colors.White;
            var selectedTextColor = Colors.White;

            // 获取所有 Category Frame/Label 
            Frame[] allFrames = { AllCategoryFrame, BreadCategoryFrame, CakeCategoryFrame, OthersCategoryFrame };
            Label[] allLabels = { AllCategoryLabel, BreadCategoryLabel, CakeCategoryLabel, OthersCategoryLabel };
            string[] categories = { "All", "Bread", "Cake", "Others" };

            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] == selectedCategory)
                {
                    // 选中项样式
                    allFrames[i].BackgroundColor = selectedColor;
                    allFrames[i].BorderColor = selectedColor;
                    allLabels[i].TextColor = selectedTextColor;
                }
                else
                {
                    // 非选中项样式
                    allFrames[i].BackgroundColor = defaultBgColor;
                    allFrames[i].BorderColor = selectedColor; // 边框颜色保持不变
                    allLabels[i].TextColor = defaultTextColor;
                }
            }
        }

        // 1. Location Tap Handler
        private async void OnMapTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Location", "Opening map location selector...", "OK");
        }

        // 2. Category Tap Handler (包含 UI 更新和滚动逻辑)
        private async void OnCategoryTapped(object sender, EventArgs e)
        {
            if (sender is VisualElement element)
            {
                // 动画效果
                await element.ScaleTo(0.95, 100, Easing.CubicOut);
                await element.ScaleTo(1, 100, Easing.CubicIn);
            }

            string category = "All";
            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                category = param;
            }

            // 1. 更新产品列表
            PopulateCategoryResults(category);

            // 2. 更新按钮样式 (高亮当前选中项)
            UpdateCategoryButtonStyle(category);

            // 3. 滚动到新显示的产品列表区域
            await ScrollViewContainer.ScrollToAsync(CategoryResultsSection, ScrollToPosition.Start, true);
        }

        // 3. Food Item Tap Handler (includes navigation to ItemDetailPage)
        // Overload for direct string parameter (used by code-generated items)
        private async void OnFoodItemTapped(object sender, string itemName)
        {
            await NavigateToItemDetail(sender, itemName);
        }

        // Overload for TappedEventArgs (used by XAML TapGestureRecognizer with CommandParameter)
        private async void OnFoodItemTapped(object sender, EventArgs e)
        {
            string itemName = "Selected Item";
            
            // Try to get parameter from TappedEventArgs
            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                itemName = param;
            }
            // Fallback: try to get from sender's BindingContext or Tag
            else if (sender is BindableObject bindable && bindable.BindingContext is string contextName)
            {
                itemName = contextName;
            }

            await NavigateToItemDetail(sender, itemName);
        }

        // --- Explicit tap handlers for 'Your Preferred' static cards ---
        private async void OnPreferredArtisanBreadTapped(object sender, EventArgs e)
            => await NavigateToItemDetail(sender, "Artisan Bread");

        private async void OnPreferredButterCroissantTapped(object sender, EventArgs e)
            => await NavigateToItemDetail(sender, "Butter Croissant");

        private async void OnPreferredChocoCakeTapped(object sender, EventArgs e)
            => await NavigateToItemDetail(sender, "Choco Cake");

        private async void OnPreferredGlazedDonutTapped(object sender, EventArgs e)
            => await NavigateToItemDetail(sender, "Glazed Donut");

        private async void OnPreferredChocoChipTapped(object sender, EventArgs e)
            => await NavigateToItemDetail(sender, "Choco Chip");

        private async void OnPreferredBlueberryMuffinTapped(object sender, EventArgs e)
            => await NavigateToItemDetail(sender, "Blueberry Muffin");

        // Common navigation logic - keep it simple and reliable
        private async Task NavigateToItemDetail(object sender, string itemName)
        {
            // Ensure a safe default name
            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = "Selected Item";
            }

            // Simple tap animation (awaited to stay on main thread)
            if (sender is VisualElement element)
            {
                await element.ScaleTo(0.95, 80, Easing.CubicOut);
                await element.ScaleTo(1, 80, Easing.CubicIn);
            }

            // Normal navigation – no extra threading tricks
            await Navigation.PushAsync(new ItemDetailPage(itemName));
        }

        // 4. Free Delivery Promo
        private async void OnFreeDeliveryTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Free Delivery", "Code 'FREESHIP30' applied to your clipboard!", "OK");
        }

        // 5. Special Offer Promo
        private async void OnSpecialOfferingTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Special Offer", "50% Discount applied to Healthy Food category.", "OK");
        }

        // 6. See All Favorites
        private async void OnSeeAllTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Preferred", "Showing all your preferred items...", "OK");
        }

        // --- Bottom Navigation Handlers ---

        // 7. Home Button
        private async void OnHomeTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Home", "You are on the Home screen.", "OK");
        }

        // 8. Cart Button (navigates to CartPage)
        private async void OnCartTapped(object sender, EventArgs e)
        {
            // 假设 CartPage 存在
            await Navigation.PushAsync(new CartPage());
        }

        // 9. Profile Navigation (navigates to ProfilePage)
        private async void OnAccountTapped(object sender, EventArgs e)
        {
            // Assumes ProfilePage exists
            await Navigation.PushAsync(new M4Food.Views.ProfilePage());
        }
    }
}