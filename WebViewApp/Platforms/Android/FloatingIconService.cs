using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Runtime;
using Microsoft.Maui.Platform;

namespace WebViewApp.Platforms.Android;

[Service(Enabled = true, Exported = false)]
public class FloatingIconService : Service
{
    private IWindowManager? _windowManager;
    private global::Android.Views.View? _floatingView;
    private WindowManagerLayoutParams? _params;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        _windowManager = GetSystemService(WindowService)?.JavaCast<IWindowManager>();

        // Create the floating view (a simple Button or ImageButton)
        var button = new global::Android.Widget.ImageButton(this);
        button.SetImageResource(global::Android.Resource.Drawable.IcDialogInfo); // Default icon
        button.SetBackgroundColor(global::Android.Graphics.Color.SlateBlue);
        button.SetPadding(20, 20, 20, 20);

        // Layout params for the overlay
        var type = Build.VERSION.SdkInt >= BuildVersionCodes.O 
            ? WindowManagerTypes.ApplicationOverlay 
            : WindowManagerTypes.Phone;

        _params = new WindowManagerLayoutParams(
            WindowManagerLayoutParams.WrapContent,
            WindowManagerLayoutParams.WrapContent,
            type,
            WindowManagerFlags.NotFocusable,
            Format.Translucent)
        {
            Gravity = GravityFlags.Top | GravityFlags.Start,
            X = 0,
            Y = 100
        };

        _floatingView = button;

        // Add touch listener for dragging and clicking
        _floatingView.SetOnTouchListener(new FloatingTouchListener(_params, _windowManager!));

        _windowManager?.AddView(_floatingView, _params);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_floatingView != null)
        {
            _windowManager?.RemoveView(_floatingView);
        }
    }

    private class FloatingTouchListener : Java.Lang.Object, global::Android.Views.View.IOnTouchListener
    {
        private readonly WindowManagerLayoutParams _params;
        private readonly IWindowManager _windowManager;
        private int _initialX;
        private int _initialY;
        private float _initialTouchX;
        private float _initialTouchY;

        public FloatingTouchListener(WindowManagerLayoutParams lp, IWindowManager wm)
        {
            _params = lp;
            _windowManager = wm;
        }

        public bool OnTouch(global::Android.Views.View? v, MotionEvent? e)
        {
            if (e == null || v == null) return false;

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _initialX = _params.X;
                    _initialY = _params.Y;
                    _initialTouchX = e.RawX;
                    _initialTouchY = e.RawY;
                    return true;
                case MotionEventActions.Move:
                    _params.X = _initialX + (int)(e.RawX - _initialTouchX);
                    _params.Y = _initialY + (int)(e.RawY - _initialTouchY);
                    _windowManager.UpdateViewLayout(v, _params);
                    return true;
                case MotionEventActions.Up:
                    float diffX = Math.Abs(e.RawX - _initialTouchX);
                    float diffY = Math.Abs(e.RawY - _initialTouchY);
                    if (diffX < 10 && diffY < 10) // Small threshold for a click
                    {
                        var service = IPlatformApplication.Current.Services.GetService<IGhostModeService>();
                        service?.ToggleGhostMode();
                    }
                    return true;
            }
            return false;
        }
    }
}
