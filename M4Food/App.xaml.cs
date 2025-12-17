using Microsoft.Maui.Controls;
using M4Food.Services;
using Plugin.Firebase.Auth;

namespace M4Food;

public partial class App : Application
{
    private readonly ILocalCacheService _localCacheService;

    public App(ILocalCacheService localCacheService)
    {
        InitializeComponent();
        _localCacheService = localCacheService;

        // Decide start page based on Firebase auth state
        var currentUser = CrossFirebaseAuth.Current.CurrentUser;

        // If a user is already authenticated, go directly to the main shell.
        // Otherwise, show the login page wrapped in a NavigationPage.
        MainPage = currentUser != null
            ? new AppShell()
            : new NavigationPage(new Views.LoginPage());

        // Initialize database
        _ = InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await _localCacheService.InitAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't prevent app from starting
            System.Diagnostics.Debug.WriteLine($"Failed to initialize database: {ex.Message}");
        }
    }
}