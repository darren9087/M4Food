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

            // Get services from DI
            _localCacheService = Application.Current?.Handler?.MauiContext?.Services
                .GetService(typeof(ILocalCacheService)) as ILocalCacheService
                ?? throw new InvalidOperationException("ILocalCacheService not registered.");

            _cloudinaryService = Application.Current?.Handler?.MauiContext?.Services
                .GetService(typeof(ICloudinaryService)) as ICloudinaryService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Load existing registration data when page appears (works offline)
            await LoadStoreRegistrationAsync();
        }

        private async Task LoadStoreRegistrationAsync()
        {
            try
            {
                var firebaseAuth = CrossFirebaseAuth.Current;
                var firebaseUser = firebaseAuth.CurrentUser;
                if (firebaseUser == null)
                {
                    return;
                }

                // Load from local cache (works offline)
                var registration = await _localCacheService.GetStoreRegistrationAsync(firebaseUser.Uid);
                if (registration != null)
                {
                    ApplyRegistrationToUi(registration);
                    System.Diagnostics.Debug.WriteLine("Store registration loaded from local cache");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading store registration: {ex.Message}");
            }
        }

        private void ApplyRegistrationToUi(StoreRegistrationDto registration)
        {
            // Populate form fields with saved data (works offline)
            StoreNameEntry.Text = registration.StoreName;
            StoreAddressEntry.Text = registration.StoreAddress;
            PhoneNumberEntry.Text = registration.PhoneNumber;

            // Load image if available (prioritize local path for offline viewing)
            if (!string.IsNullOrEmpty(registration.StoreImageLocalPath) && File.Exists(registration.StoreImageLocalPath))
            {
                // Use local image (works offline)
                StoreImagePreview.Source = ImageSource.FromFile(registration.StoreImageLocalPath);
                UploadTextLabel.Text = "Photo Loaded";
                _selectedImageLocalPath = registration.StoreImageLocalPath;
                _selectedImageUrl = registration.StoreImageUrl; // Keep URL for reference
                _selectedImagePublicId = registration.StoreImagePublicId;
            }
            else if (!string.IsNullOrEmpty(registration.StoreImageUrl))
            {
                // Fallback to URL if local path not available (requires internet)
                StoreImagePreview.Source = ImageSource.FromUri(new Uri(registration.StoreImageUrl));
                UploadTextLabel.Text = "Photo Loaded";
                _selectedImageUrl = registration.StoreImageUrl;
                _selectedImagePublicId = registration.StoreImagePublicId;
                
                // Try to download and cache the image for offline access (background task)
                _ = DownloadAndCacheImageAsync(registration.StoreImageUrl, registration.Id);
            }
        }

        private async Task DownloadAndCacheImageAsync(string imageUrl, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl)) return;

                // Download image to local cache for offline access
                var localPath = Path.Combine(FileSystem.AppDataDirectory, "store_images", $"{userId}_{Path.GetFileName(imageUrl)}");
                var directory = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(localPath, imageBytes);

                // Update registration with local path for offline access
                var registration = await _localCacheService.GetStoreRegistrationAsync(userId);
                if (registration != null)
                {
                    registration.StoreImageLocalPath = localPath;
                    await _localCacheService.SaveStoreRegistrationAsync(registration);
                    System.Diagnostics.Debug.WriteLine($"Store image cached locally for offline access: {localPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to download and cache image: {ex.Message}");
                // Continue without caching - URL will still work when online
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Handler for the Photo Upload Frame
        private async void OnUploadPhotoTapped(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Upload Store Photo", "Cancel", null, "Take Photo", "Choose from Gallery");

            if (action == "Take Photo")
            {
#if ANDROID
                // Request camera permission
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required", 
                        "Camera permission is required to take photos. Please enable it in app settings.", 
                        "OK");
                    return;
                }
#endif
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        await LoadSelectedPhotoAsync(photo);
                    }
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
                        System.Diagnostics.Debug.WriteLine($"Gallery permission status (DonationPage): {galleryStatus}");
                        if (galleryStatus != PermissionStatus.Granted)
                        {
                            await DisplayAlert("Permission Required",
                                "Permission to access photos is required to select images from gallery. Please enable it in app settings.",
                                "OK");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error requesting gallery permission (DonationPage): {ex}");
                    }
#endif

                    System.Diagnostics.Debug.WriteLine("Starting PickPhotoAsync (DonationPage)...");
                    await AppendDebugLogAsync("Starting PickPhotoAsync (DonationPage)...");
                    var pickTask = MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                    {
                        Title = "Select a store photo"
                    });

                    var completed = await Task.WhenAny(pickTask, Task.Delay(TimeSpan.FromSeconds(60)));
                    if (completed != pickTask)
                    {
                        System.Diagnostics.Debug.WriteLine("PickPhotoAsync is taking longer than 60s (DonationPage).");
                        await AppendDebugLogAsync("PickPhotoAsync timeout (DonationPage, 60s)");
                        await DisplayAlert("Please Wait", "Gallery picker is taking longer than expected. Please try again.", "OK");
                    }
                    else
                    {
                        var photo = await pickTask;
                        if (photo != null)
                        {
                            await LoadSelectedPhotoAsync(photo);
                        }
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
                // Save to local cache first
                var localPath = Path.Combine(FileSystem.AppDataDirectory, "store_images", $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}");
                var directory = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                try
                {
                    // Defer heavy I/O: copy selected photo to local path on a background thread,
                    // but don't perform any synchronous upload here to avoid blocking the activity result path.
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Copy the picked photo to local path on a background thread using async I/O and explicit flush.
                            await using (var sourceStream = await photo.OpenReadAsync())
                            {
                                await using (var destStream = new FileStream(
                                    localPath,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.ReadWrite,
                                    bufferSize: 81920,
                                    useAsync: true))
                                {
                                    await sourceStream.CopyToAsync(destStream);
                                    await destStream.FlushAsync();
                                }
                            }

                            // Update UI/state on main thread when copy finishes
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                _selectedImageLocalPath = localPath;
                                StoreImagePreview.Source = ImageSource.FromFile(localPath);
                                UploadTextLabel.Text = "Photo Selected!";
                            });

                            await AppendDebugLogAsync($"Background: copied selected store photo to {localPath}");
                        }
                        catch (Exception bgEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Background copy failed (DonationPage): {bgEx}");
                            await AppendDebugLogAsync($"Background copy failed (DonationPage): {bgEx}");
                        }
                    });
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Permission error opening photo: {uaEx}");
                    await AppendDebugLogAsync($"Permission error opening photo: {uaEx}");
                    await DisplayAlert("Permission Error", $"Cannot access selected photo: {uaEx.Message}", "OK");
                    return;
                }
                catch (OperationCanceledException ocEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Operation cancelled while handling photo: {ocEx}");
                    await AppendDebugLogAsync($"Operation cancelled while handling photo: {ocEx}");
                    await DisplayAlert("Cancelled", "Photo selection was cancelled.", "OK");
                    return;
                }
                catch (System.IO.IOException ioEx)
                {
                    System.Diagnostics.Debug.WriteLine($"I/O error opening photo: {ioEx}");
                    await AppendDebugLogAsync($"I/O error opening photo: {ioEx}");
                    await DisplayAlert("I/O Error", $"Failed to read selected photo: {ioEx.Message}", "OK");
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error handling photo: {ex}");
                    await AppendDebugLogAsync($"Unexpected error handling photo: {ex}");
                    var shortMsg = ex.Message.Length > 300 ? ex.Message.Substring(0, 300) + "..." : ex.Message;
                    await DisplayAlert("Photo Error", $"Failed to process selected photo: {shortMsg}", "OK");
                    return;
                }

                // Upload to Cloudinary if service is available (run upload in background to avoid blocking UI)
                if (_cloudinaryService != null)
                {
                    try
                    {
                        var uploadTask = Task.Run(async () =>
                        {
                            // Try direct upload from picker stream first (avoids disk I/O and file locks).
                            if (_cloudinaryService != null)
                            {
                                try
                                {
                                    await using var directStream = await photo.OpenReadAsync();
                                    var fileName = photo.FileName ?? $"store_{DateTime.UtcNow.Ticks}.jpg";
                                    return await _cloudinaryService.UploadImageStreamAsync(
                                        directStream,
                                        fileName,
                                        folder: "m4food/stores"
                                    );
                                }
                                catch (Exception exDirect)
                                {
                                    // Log and fall back to file-based upload
                                    await AppendDebugLogAsync($"DonationPage: direct stream upload failed, falling back to file upload: {exDirect}");
                                }
                            }

                            const int maxAttempts = 3;
                            Exception? lastEx = null;
                            for (int attempt = 1; attempt <= maxAttempts; attempt++)
                            {
                                try
                                {
                                    await using var uploadStream = new FileStream(
                                        localPath,
                                        FileMode.Open,
                                        FileAccess.Read,
                                        FileShare.ReadWrite,
                                        bufferSize: 81920,
                                        useAsync: true);

                                    var fileName = photo.FileName ?? $"store_{DateTime.UtcNow.Ticks}.jpg";
                                    return await _cloudinaryService.UploadImageStreamAsync(
                                        uploadStream,
                                        fileName,
                                        folder: "m4food/stores"
                                    );
                                }
                                catch (IOException ioEx)
                                {
                                    lastEx = ioEx;
                                    await Task.Delay(200);
                                }
                            }

                            throw lastEx ?? new IOException("Failed to open file for upload.");
                        });

                        var (url, publicId) = await uploadTask;
                        _selectedImageUrl = url;
                        _selectedImagePublicId = publicId;
                        System.Diagnostics.Debug.WriteLine($"Store image uploaded to Cloudinary: {url}");
                        await AppendDebugLogAsync($"Store image uploaded to Cloudinary: {url}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to upload to Cloudinary: {ex.Message}");
                        await AppendDebugLogAsync($"Failed to upload to Cloudinary: {ex}");
                        // Continue without Cloudinary URL - local path is saved
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
                // Validate inputs
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

                // Check if registration already exists to preserve CreatedAt
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
                    CreatedAt = existingRegistration?.CreatedAt ?? DateTime.UtcNow, // Preserve original creation date
                    UpdatedAt = DateTime.UtcNow
                };

                // Save to local cache (works offline) - automatically saves for offline viewing
                await _localCacheService.SaveStoreRegistrationAsync(registration);
                System.Diagnostics.Debug.WriteLine("Store registration saved to local cache (offline-first)");

                await DisplayAlert("Success", 
                    "Store registered successfully! Your information has been saved and will be available offline.", 
                    "OK");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write debug log (DonationPage): {ex}");
            }
        }
    }
}