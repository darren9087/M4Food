#if ANDROID
using System;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;

namespace M4Food;

public static class NotificationHelper
{
    private const string ChannelId = "m4food_default_channel";
    private const string ChannelName = "M4Food";

    private static bool _channelCreated;

    public static void ShowNotification(string title, string message)
    {
        var context = Platform.AppContext;
        if (context == null)
            return;

        // Create notification channel (Android 8+)
        if (!_channelCreated && Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                ChannelName,
                NotificationImportance.Default)
            {
                Description = "General notifications for M4Food"
            };

            var manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
            manager?.CreateNotificationChannel(channel);
            _channelCreated = true;
        }

        // Build notification
        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true);

        // Show notification
        NotificationManagerCompat
            .From(context)
            .Notify(new Random().Next(), builder.Build());
    }
}
#endif


