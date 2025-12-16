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
        try
        {
            var firebaseAuth = CrossFirebaseAuth.Current;
            var firebaseUser = firebaseAuth.CurrentUser;
            if (firebaseUser == null)
            {
                return;
            }

            var tokenResult = await firebaseUser.GetIdTokenResultAsync(false);
            var idToken = tokenResult.Token;
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return;
            }

            // First try to load from local cache
            var localProfile = await _profileService.GetUserProfileAsync(firebaseUser.Uid);
            if (localProfile != null)
            {
                ApplyProfileToUi(localProfile);
            }

            // Then try to sync from Firebase Realtime Database (cloud profile)
            var url = $"users/{firebaseUser.Uid}.json?auth={idToken}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var remoteProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                if (remoteProfile != null)
                {
                    ApplyProfileToUi(remoteProfile);
                    // keep local cache in sync
                    await _profileService.SaveUserProfileAsync(remoteProfile);
                }
            }
        }
        catch
        {
            // Ignore errors on initial load; user can still edit and save
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

            var tokenResult = await firebaseUser.GetIdTokenResultAsync(false);
            var idToken = tokenResult.Token;
            if (string.IsNullOrWhiteSpace(idToken))
            {
                await DisplayAlert("Error", "Failed to get ID token.", "OK");
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

            // 1. 先保存到本地 + Cloudinary（如果以后有头像）
            var savedProfile = await _profileService.SaveProfileAsync(
                profile,
                avatarStream: null,
                avatarFileName: null);

            // 2. Sync to Firebase Realtime Database (cloud)
            var url = $"users/{firebaseUser.Uid}.json?auth={idToken}";
            var response = await _httpClient.PutAsJsonAsync(url, savedProfile);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert(
                    "Warning",
                    $"Profile saved locally but sync failed ({(int)response.StatusCode}).",
                    "OK");
                return;
            }

            await DisplayAlert("Success", "Profile saved.", "OK");
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
    }
}