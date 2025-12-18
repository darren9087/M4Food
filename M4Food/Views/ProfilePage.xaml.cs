using System.Net.Http.Headers;
using System.Net.Http.Json;
using M4Food.Models.DTOs;
using M4Food.Services;
using Plugin.Firebase.Auth;

namespace M4Food.Views;

public partial class ProfilePage : ContentPage
{
    // A "Database" of countries, flags, and phone codes
    private readonly Dictionary<string, (string Name, string Code)> CountryData = new()
    {
        // ASEAN
        { "🇸🇬", ("Singapore", "+65") },
        { "🇹🇭", ("Thailand", "+66") },
        { "🇵🇭", ("Philippines", "+63") },
        { "🇻🇳", ("Vietnam", "+84") },
        { "🇧🇳", ("Brunei", "+673") },
        { "🇲🇲", ("Myanmar", "+95") },
        { "🇰🇭", ("Cambodia", "+855") },
        { "🇱🇦", ("Laos", "+856") },
        { "🇲🇾", ("Malaysia", "+60") },
        { "🇮🇩", ("Indonesia", "+62") }, 

        // East Asia
        { "🇨🇳", ("China", "+86") },
        { "🇯🇵", ("Japan", "+81") },
        { "🇰🇷", ("South Korea", "+82") },

        // Oceania & South Asia
        { "🇮🇳", ("India", "+91") },
        { "🇦🇺", ("Australia", "+61") },
        { "🇳🇿", ("New Zealand", "+64") },

        // Americas & Europe
        { "🇺🇸", ("United States", "+1") },
        { "🇨🇦", ("Canada", "+1") },
        { "🇬🇧", ("United Kingdom", "+44") }
    };

    private readonly IProfileService _profileService;
    private readonly HttpClient _httpClient;

    public ProfilePage()
    {
        InitializeComponent();

        _profileService = Application.Current
            .Handler?
            .MauiContext?
            .Services
            .GetService(typeof(IProfileService)) as IProfileService
            ?? throw new InvalidOperationException("IProfileService not registered.");

        _httpClient = new HttpClient
        {
            // Point to Firebase Realtime Database (replace with your database URL if different)
            BaseAddress = new Uri("https://m4food-default-rtdb.asia-southeast1.firebasedatabase.app/")
        };

        // 1. Populate the Picker with "Flag + Name" ONLY (No Phone Code)
        // Example: "🇲🇾 Malaysia"
        foreach (var item in CountryData)
        {
            string flag = item.Key;
            string name = item.Value.Name;

            // Add just the Flag and Name to the dropdown
            FlagPicker.Items.Add($"{flag} {name}");
        }

        // 2. Set Default to Malaysia
        var defaultItem = FlagPicker.Items.FirstOrDefault(x => x.StartsWith("🇲🇾"));
        if (defaultItem != null)
        {
            FlagPicker.SelectedItem = defaultItem;
        }

        // Load existing profile when the page appears
        this.Loaded += async (_, _) => await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var firebaseAuth = CrossFirebaseAuth.Current;
        var firebaseUser = firebaseAuth.CurrentUser;
        if (firebaseUser == null)
        {
            return;
        }

        // 1. Load from local cache FIRST (works offline, instant)
        try
        {
            var localProfile = await _profileService.GetUserProfileAsync(firebaseUser.Uid);
            if (localProfile != null)
            {
                ApplyProfileToUi(localProfile);
                System.Diagnostics.Debug.WriteLine("Profile loaded from local cache");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading local profile: {ex.Message}");
        }

        // 2. Try to sync from Firebase in background (don't block if offline)
        _ = SyncProfileFromFirebaseAsync(firebaseUser.Uid);
    }

    /// <summary>
    /// Background sync: fetches profile from Firebase and updates UI/cache
    /// </summary>
    private async Task SyncProfileFromFirebaseAsync(string userId)
    {
        try
        {
            var firebaseUser = CrossFirebaseAuth.Current.CurrentUser;
            if (firebaseUser == null) return;

            var tokenResult = await firebaseUser.GetIdTokenResultAsync(false);
            var idToken = tokenResult.Token;
            if (string.IsNullOrWhiteSpace(idToken)) return;

            var url = $"users/{userId}.json?auth={idToken}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var remoteProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                if (remoteProfile != null)
                {
                    // Update UI on main thread
                    await MainThread.InvokeOnMainThreadAsync(() => ApplyProfileToUi(remoteProfile));
                    
                    // Update local cache
                    await _profileService.SaveUserProfileAsync(remoteProfile);
                    System.Diagnostics.Debug.WriteLine("Profile synced from Firebase");
                }
            }
        }
        catch (Exception ex)
        {
            // Offline or network error - that's OK, we already have local data
            System.Diagnostics.Debug.WriteLine($"Profile sync from Firebase failed (offline?): {ex.Message}");
        }
    }

    private void ApplyProfileToUi(UserProfileDto profile)
    {
        FullNameEntry.Text = profile.FullName;
        EmailEntry.Text = profile.Email;
        PhoneCodeLabel.Text = profile.CountryCode ?? "+60";
        PhoneEntry.Text = profile.PhoneNumber;
        CountryEntry.Text = profile.Country ?? "Malaysia";

        if (!string.IsNullOrWhiteSpace(profile.Gender))
        {
            var genderOption = GenderPicker.ItemsSource?
                .OfType<string>()
                .FirstOrDefault(g => string.Equals(g, profile.Gender, StringComparison.OrdinalIgnoreCase));

            if (genderOption != null)
            {
                GenderPicker.SelectedItem = genderOption;
            }
        }

        AddressEditor.Text = profile.Address;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
        if (answer)
        {
            await Navigation.PushAsync(new LoginPage());
        }
    }

    private void OnFlagChanged(object sender, EventArgs e)
    {
        if (FlagPicker.SelectedItem is string selectedString)
        {
            // The string is "🇲🇾 Malaysia"
            // We split it by space to get the flag (the first part)
            string[] parts = selectedString.Split(' ');

            if (parts.Length > 0)
            {
                string selectedFlag = parts[0];

                if (CountryData.ContainsKey(selectedFlag))
                {
                    var data = CountryData[selectedFlag];

                    // Update the labels
                    PhoneCodeLabel.Text = data.Code; // Shows code separately
                    CountryEntry.Text = data.Name;
                }
            }
        }
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        try
        {
            var firebaseAuth = CrossFirebaseAuth.Current;
            var firebaseUser = firebaseAuth.CurrentUser;
            if (firebaseUser == null)
            {
                await DisplayAlert("Error", "Please sign in first.", "OK");
                return;
            }

            var profile = new UserProfileDto
            {
                Id = firebaseUser.Uid,
                FullName = FullNameEntry.Text ?? string.Empty,
                Email = string.IsNullOrWhiteSpace(EmailEntry.Text)
                    ? (firebaseUser.Email ?? string.Empty)
                    : EmailEntry.Text!,
                CountryCode = PhoneCodeLabel.Text,
                PhoneNumber = PhoneEntry.Text,
                Country = CountryEntry.Text,
                Gender = GenderPicker.SelectedItem?.ToString(),
                Address = AddressEditor.Text,
                AvatarUrl = null,
                AvatarPublicId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 1. Save to local cache FIRST (works offline)
            var savedProfile = await _profileService.SaveProfileAsync(
                profile,
                avatarStream: null,
                avatarFileName: null);

            System.Diagnostics.Debug.WriteLine("Profile saved to local cache");

            // 2. Try to sync to Firebase in background (don't block if offline)
            _ = SyncProfileToFirebaseAsync(firebaseUser.Uid, savedProfile);

            await DisplayAlert("Success", "Profile saved.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Background sync: pushes profile to Firebase
    /// </summary>
    private async Task SyncProfileToFirebaseAsync(string userId, UserProfileDto profile)
    {
        try
        {
            var firebaseUser = CrossFirebaseAuth.Current.CurrentUser;
            if (firebaseUser == null) return;

            var tokenResult = await firebaseUser.GetIdTokenResultAsync(false);
            var idToken = tokenResult.Token;
            if (string.IsNullOrWhiteSpace(idToken)) return;

            var url = $"users/{userId}.json?auth={idToken}";
            var response = await _httpClient.PutAsJsonAsync(url, profile);

            if (response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine("Profile synced to Firebase");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Profile sync failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Offline or network error - that's OK, profile is saved locally
            System.Diagnostics.Debug.WriteLine($"Profile sync to Firebase failed (offline?): {ex.Message}");
        }
    }
}