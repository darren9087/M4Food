using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.Auth;
using M4Food.Services;
#if ANDROID
using Plugin.Firebase.Auth.Google;
using Plugin.Firebase.Core.Platforms.Android;
#endif

namespace M4Food;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .RegisterFirebaseServices()
            .RegisterServices();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
        // Register local storage service
        builder.Services.AddSingleton<ILocalCacheService, LocalCacheService>();
        
        // Register Cloudinary service
        builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
        
        // Register offline map tile cache service
        builder.Services.AddSingleton<IMapTileCacheService, MapTileCacheService>();
        
        // Register offline route service
        builder.Services.AddSingleton<IOfflineRouteService, OfflineRouteService>();
        
        return builder;
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
#if ANDROID
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddAndroid(android =>
            {
                android.OnCreate((activity, _) =>
                {
                    CrossFirebase.Initialize(activity);
                    var clientId = activity.GetString(Resource.String.default_web_client_id);
                    FirebaseAuthGoogleImplementation.Initialize(clientId);
                });

                android.OnActivityResult((activity, requestCode, resultCode, data) =>
                {
                    _ = FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
                });
            });
        });
#endif

        return builder;
    }
}