using Android.App;
using Android.Content;

namespace WebViewApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class GhostModeReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == "DISABLE_GHOST_MODE")
        {
            var service = IPlatformApplication.Current.Services.GetService<IGhostModeService>();
            service?.DisableGhostMode();
        }
    }
}
