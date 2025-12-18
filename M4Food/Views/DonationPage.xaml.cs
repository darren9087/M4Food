using Microsoft.Maui.Controls;

namespace M4Food.Views
{
    public partial class DonationPage : ContentPage
    {
        public DonationPage()
        {
            InitializeComponent();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Handler for the Photo Upload Frame
        private async void OnUploadPhotoTapped(object sender, EventArgs e)
        {
            // In a real app, you would use MediaPicker here to open the gallery
            string action = await DisplayActionSheet("Upload Store Photo", "Cancel", null, "Take Photo", "Choose from Gallery");

            if (action == "Take Photo" || action == "Choose from Gallery")
            {
                // Simulating a successful upload for UI demonstration
                UploadTextLabel.Text = "Photo Selected!";
                StoreImagePreview.Source = "store_placeholder.png"; // Make sure you have a placeholder image or remove this line if checking virtually
                await DisplayAlert("Success", "Photo uploaded successfully!", "OK");
            }
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Success", "Store registered successfully!", "OK");
            await Navigation.PopAsync();
        }
    }
}