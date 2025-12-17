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

    public ProfilePage()
    {
        InitializeComponent();

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
}