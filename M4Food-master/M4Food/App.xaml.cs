using Microsoft.Maui.Controls;
using M4Food.Services;

namespace M4Food;

public partial class App : Application
{
    private readonly ILocalCacheService _localCacheService;

    public App(ILocalCacheService localCacheService)
    {
        InitializeComponent();
        _localCacheService = localCacheService;
        MainPage = new AppShell();
        
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