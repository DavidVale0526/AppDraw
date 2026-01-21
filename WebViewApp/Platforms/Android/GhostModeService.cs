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
    private bool _isGhostModeActive = false;
    private bool _isFloatingIconActive = false;
    private float _opacity = 0.5f;

    public float Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            if (_isGhostModeActive) ApplyOpacity(_opacity);
        }
    }

    public async Task EnableGhostMode()
    {
        if (_isGhostModeActive) return;

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

            ApplyOpacityInternal(activity, _opacity);

            _isGhostModeActive = true;
            ShowNotification(activity);
        });
    }

    private void ApplyOpacity(float alpha)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;
        activity.RunOnUiThread(() => ApplyOpacityInternal(activity, alpha));
    }

    private void ApplyOpacityInternal(Activity activity, float alpha)
    {
        var attributes = activity.Window.Attributes;
        if (attributes != null)
        {
            attributes.Alpha = alpha;
            activity.Window.Attributes = attributes;
        }
    }

    public void DisableGhostMode()
    {
        if (!_isGhostModeActive) return;

        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;

        activity.RunOnUiThread(() =>
        {
            activity.Window.ClearFlags(WindowManagerFlags.NotTouchable);
            activity.Window.ClearFlags(WindowManagerFlags.NotFocusable);

            var attributes = activity.Window.Attributes;
            attributes.Alpha = 1.0f;
            activity.Window.Attributes = attributes;

            _isGhostModeActive = false;
            CancelNotification(activity);
        });
    }

    public void ToggleGhostMode()
    {
        if (_isGhostModeActive)
            DisableGhostMode();
        else
            _ = EnableGhostMode();
    }

    public void RequestOverlayPermission()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;

        if (!global::Android.Provider.Settings.CanDrawOverlays(activity))
        {
            var intent = new Intent(global::Android.Provider.Settings.ActionManageOverlayPermission,
                global::Android.Net.Uri.Parse("package:" + activity.PackageName));
            activity.StartActivity(intent);
        }
        else
        {
            _ = ToggleFloatingIconInternal(activity);
        }
    }

    public void ToggleFloatingIcon()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;
        _ = ToggleFloatingIconInternal(activity);
    }

    private async Task ToggleFloatingIconInternal(Activity activity)
    {
        if (_isFloatingIconActive)
        {
            StopFloatingIcon(activity);
            _isFloatingIconActive = false;
        }
        else
        {
            if (!global::Android.Provider.Settings.CanDrawOverlays(activity))
            {
                RequestOverlayPermission();
                return;
            }
            StartFloatingIcon(activity);
            _isFloatingIconActive = true;
        }
    }

    private void StartFloatingIcon(Activity activity)
    {
        var intent = new Intent(activity, typeof(FloatingIconService));
        activity.StartService(intent);
    }

    private void StopFloatingIcon(Activity activity)
    {
        var intent = new Intent(activity, typeof(FloatingIconService));
        activity.StopService(intent);
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
