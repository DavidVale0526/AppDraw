using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Microsoft.Maui.Platform;
using AndroidX.Core.App;

namespace WebViewApp.Platforms.Android;

public class GhostModeService : IGhostModeService
{
    private const string ChannelId = "ghost_mode_channel";
    private const int NotificationId = 1001;

    public async Task EnableGhostMode()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;

        // Request notification permission for Android 13+
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
            if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
            {
                status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.PostNotifications>();
            }

            if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
            {
                return;
            }
        }

        activity.RunOnUiThread(() =>
        {
            activity.Window.AddFlags(WindowManagerFlags.NotTouchable);
            activity.Window.AddFlags(WindowManagerFlags.NotFocusable);

            var attributes = activity.Window.Attributes;
            attributes.Alpha = 0.5f;
            activity.Window.Attributes = attributes;

            ShowNotification(activity);
        });
    }

    public void DisableGhostMode()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;

        activity.RunOnUiThread(() =>
        {
            activity.Window.ClearFlags(WindowManagerFlags.NotTouchable);
            activity.Window.ClearFlags(WindowManagerFlags.NotFocusable);

            var attributes = activity.Window.Attributes;
            attributes.Alpha = 1.0f;
            activity.Window.Attributes = attributes;

            CancelNotification(activity);
        });
    }

    private void ShowNotification(Activity activity)
    {
        var intent = new Intent(activity, typeof(GhostModeReceiver));
        intent.SetAction("DISABLE_GHOST_MODE");
        var pendingIntent = PendingIntent.GetBroadcast(activity, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notificationManager = (NotificationManager)activity.GetSystemService(Context.NotificationService);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "Ghost Mode", NotificationImportance.High)
            {
                Description = "Ghost Mode Status",
                LockscreenVisibility = NotificationVisibility.Public
            };
            notificationManager.CreateNotificationChannel(channel);
        }

        var notification = new NotificationCompat.Builder(activity, ChannelId)
            .SetContentTitle("Modo Fantasma Activo")
            .SetContentText("Toca 'Restaurar' para volver a la normalidad.")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .AddAction(global::Android.Resource.Drawable.IcMenuRevert, "Restaurar", pendingIntent)
            .Build();

        notificationManager.Notify(NotificationId, notification);
    }

    private void CancelNotification(Activity activity)
    {
        var notificationManager = (NotificationManager)activity.GetSystemService(Context.NotificationService);
        notificationManager.Cancel(NotificationId);
    }
}
