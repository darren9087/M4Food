using Microsoft.Maui.Controls;
using M4Food.Services;
using M4Food.Models.DTOs;
using Plugin.Firebase.Auth;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
#if ANDROID
using Microsoft.Maui.ApplicationModel;
#endif
using System.Net.Http;
using System.IO;
using System;
using System.Threading.Tasks;

namespace M4Food.Views
{
    public partial class DonationPage : ContentPage
    {
        private readonly ILocalCacheService _localCacheService;
        private readonly ICloudinaryService? _cloudinaryService;
        private string? _selectedImageLocalPath;
        private string? _selectedImageUrl;
        private string? _selectedImagePublicId;

        public DonationPage()
        {
            InitializeComponent();

            _localCacheService = Application.Current?.Handler?.MauiContext?.Services
                .GetService(typeof(ILocalCacheService)) as ILocalCacheService
                ?? throw new InvalidOperationException("ILocalCacheService not registered.");

            _cloudinaryService = Application.Current?.Handler?.MauiContext?.Services
                .GetService(typeof(ICloudinaryService)) as ICloudinaryService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadStoreRegistrationAsync();
        }

        private async Task LoadStoreRegistrationAsync()
        {
            try
            {
                var firebaseAuth = CrossFirebaseAuth.Current;
                var firebaseUser = firebaseAuth.CurrentUser;
                if (firebaseUser == null) return;

                var registration = await _localCacheService.GetStoreRegistrationAsync(firebaseUser.Uid);
                if (registration != null)
                {
                    ApplyRegistrationToUi(registration);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading store registration: {ex}");
            }
        }

        private void ApplyRegistrationToUi(StoreRegistrationDto registration)
        {
            StoreNameEntry.Text = registration.StoreName;
            StoreAddressEntry.Text = registration.StoreAddress;
            PhoneNumberEntry.Text = registration.PhoneNumber;

            if (!string.IsNullOrEmpty(registration.StoreImageLocalPath) && File.Exists(registration.StoreImageLocalPath))
            {
                StoreImagePreview.Source = ImageSource.FromFile(registration.StoreImageLocalPath);
                UploadTextLabel.Text = "Photo Loaded";
                _selectedImageLocalPath = registration.StoreImageLocalPath;
                _selectedImageUrl = registration.StoreImageUrl;
                _selectedImagePublicId = registration.StoreImagePublicId;
            }
            else if (!string.IsNullOrEmpty(registration.StoreImageUrl))
            {
                StoreImagePreview.Source = ImageSource.FromUri(new Uri(registration.StoreImageUrl));
                UploadTextLabel.Text = "Photo Loaded";
                _selectedImageUrl = registration.StoreImageUrl;
                _selectedImagePublicId = registration.StoreImagePublicId;
                _ = DownloadAndCacheImageAsync(registration.StoreImageUrl, registration.Id);
            }
        }

        private async Task DownloadAndCacheImageAsync(string imageUrl, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl)) return;
                var localPath = Path.Combine(FileSystem.AppDataDirectory, "store_images", $"{userId}_{Path.GetFileName(imageUrl)}");
                var dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                using var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(localPath, bytes);

                var reg = await _localCacheService.GetStoreRegistrationAsync(userId);
                if (reg != null)
                {
                    reg.StoreImageLocalPath = localPath;
                    await _localCacheService.SaveStoreRegistrationAsync(reg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cache image: {ex}");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e) => await Navigation.PopAsync();

        private async void OnUploadPhotoTapped(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Upload Store Photo", "Cancel", null, "Take Photo", "Choose from Gallery");
            if (action == "Take Photo")
            {
#if ANDROID
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", "Camera permission is required to take photos.", "OK");
                    return;
                }
#endif
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null) await LoadSelectedPhotoAsync(photo);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to capture photo: {ex.Message}", "OK");
                }
            }
            else if (action == "Choose from Gallery")
            {
                try
                {
#if ANDROID
                    try
                    {
                        var galleryStatus = await Permissions.RequestAsync<Permissions.Photos>();
                        if (galleryStatus != PermissionStatus.Granted)
                        {
                            await DisplayAlert("Permission Required", "Permission to access photos is required.", "OK");
                            return;
                        }
                    }
                    catch { /* ignore */ }
#endif
                    var pickTask = MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Select a store photo" });
                    var completed = await Task.WhenAny(pickTask, Task.Delay(TimeSpan.FromSeconds(60)));
                    if (completed != pickTask)
                    {
                        await AppendDebugLogAsync("PickPhotoAsync timeout (DonationPage, 60s)");
                        await DisplayAlert("Please Wait", "Gallery picker is taking longer than expected.", "OK");
                    }
                    else
                    {
                        var photo = await pickTask;
                        if (photo != null) await LoadSelectedPhotoAsync(photo);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to pick photo: {ex.Message}", "OK");
                }
            }
        }

        private async Task LoadSelectedPhotoAsync(FileResult photo)
        {
            try
            {
                var localPath = Path.Combine(FileSystem.AppDataDirectory, "store_images", $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}");
                var dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                // copy in background and update UI when done
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await using var src = await photo.OpenReadAsync();
                        await using var dst = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 81920, useAsync: true);
                        await src.CopyToAsync(dst);
                        await dst.FlushAsync();
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _selectedImageLocalPath = localPath;
                            StoreImagePreview.Source = ImageSource.FromFile(localPath);
                            UploadTextLabel.Text = "Photo Selected!";
                        });
                        await AppendDebugLogAsync($"Background: copied selected store photo to {localPath}");
                    }
                    catch (Exception ex)
                    {
                        await AppendDebugLogAsync($"Background copy failed (DonationPage): {ex}");
                    }
                });

                if (_cloudinaryService != null)
                {
                    try
                    {
                        // try direct stream upload first
                        await using var direct = await photo.OpenReadAsync();
                        try
                        {
                            var fileName = photo.FileName ?? $"store_{DateTime.UtcNow.Ticks}.jpg";
                            var (url, publicId) = await _cloudinaryService.UploadImageStreamAsync(direct, fileName, folder: "m4food/stores");
                            _selectedImageUrl = url;
                            _selectedImagePublicId = publicId;
                            await AppendDebugLogAsync($"Store image uploaded to Cloudinary: {url}");
                            return;
                        }
                        catch (Exception exDirect)
                        {
                            await AppendDebugLogAsync($"Direct upload failed, will try file fallback: {exDirect}");
                        }

                        // fallback: wait until file exists then upload from file with retries
                        const int maxAttempts = 3;
                        for (int attempt = 1; attempt <= maxAttempts; attempt++)
                        {
                            try
                            {
                                await using var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, useAsync: true);
                                var fileName = photo.FileName ?? $"store_{DateTime.UtcNow.Ticks}.jpg";
                                var (url, publicId) = await _cloudinaryService.UploadImageStreamAsync(fileStream, fileName, folder: "m4food/stores");
                                _selectedImageUrl = url;
                                _selectedImagePublicId = publicId;
                                await AppendDebugLogAsync($"Store image uploaded to Cloudinary (fallback): {url}");
                                break;
                            }
                            catch (IOException) when (attempt < maxAttempts)
                            {
                                await Task.Delay(200);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await AppendDebugLogAsync($"Failed to upload to Cloudinary: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to process photo: {ex.Message}", "OK");
            }
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(StoreNameEntry.Text))
                {
                    await DisplayAlert("Validation Error", "Please enter store name.", "OK");
                    return;
                }
                if (string.IsNullOrWhiteSpace(StoreAddressEntry.Text))
                {
                    await DisplayAlert("Validation Error", "Please enter store address.", "OK");
                    return;
                }
                if (string.IsNullOrWhiteSpace(PhoneNumberEntry.Text))
                {
                    await DisplayAlert("Validation Error", "Please enter phone number.", "OK");
                    return;
                }

                var firebaseAuth = CrossFirebaseAuth.Current;
                var firebaseUser = firebaseAuth.CurrentUser;
                string userId = firebaseUser?.Uid ?? Guid.NewGuid().ToString();

                var existingRegistration = await _localCacheService.GetStoreRegistrationAsync(userId);
                var registration = new StoreRegistrationDto
                {
                    Id = userId,
                    StoreName = StoreNameEntry.Text.Trim(),
                    StoreAddress = StoreAddressEntry.Text.Trim(),
                    PhoneNumber = PhoneNumberEntry.Text.Trim(),
                    StoreImageUrl = _selectedImageUrl,
                    StoreImageLocalPath = _selectedImageLocalPath,
                    StoreImagePublicId = _selectedImagePublicId,
                    CreatedAt = existingRegistration?.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _localCacheService.SaveStoreRegistrationAsync(registration);
                await DisplayAlert("Success", "Store registered successfully! Your information has been saved and will be available offline.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to register store: {ex.Message}", "OK");
            }
        }

        private async Task AppendDebugLogAsync(string message)
        {
            try
            {
                var logDir = FileSystem.AppDataDirectory;
                var logPath = Path.Combine(logDir, "photo_debug.log");
                var entry = $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}";
                await File.AppendAllTextAsync(logPath, entry);
            }
            catch { }
        }
    }
}

