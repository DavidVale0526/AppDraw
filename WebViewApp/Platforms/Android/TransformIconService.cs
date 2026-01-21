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
public class TransformIconService : Service
{
    private IWindowManager? _windowManager;
    private global::Android.Views.View? _floatingView;
    private WindowManagerLayoutParams? _params;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate()
    {
        base.OnCreate();
        _windowManager = GetSystemService(WindowService)?.JavaCast<IWindowManager>();

        // Create the floating view for transform mode
        var button = new global::Android.Widget.ImageButton(this);
        button.SetImageResource(global::Android.Resource.Drawable.IcMenuRotate); // Rotate icon
        button.SetBackgroundColor(global::Android.Graphics.Color.DarkOrange);
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
            Gravity = GravityFlags.Top | GravityFlags.End,
            X = 0,
            Y = 200
        };

        _floatingView = button;

        // Add touch listener for dragging and clicking
        _floatingView.SetOnTouchListener(new TransformTouchListener(_params, _windowManager!, this));

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

    private class TransformTouchListener : Java.Lang.Object, global::Android.Views.View.IOnTouchListener
    {
        private readonly WindowManagerLayoutParams _params;
        private readonly IWindowManager _windowManager;
        private readonly Context _context;
        private int _initialX;
        private int _initialY;
        private float _initialTouchX;
        private float _initialTouchY;
        
        private const int MOVEMENT_THRESHOLD = 15;

        public TransformTouchListener(WindowManagerLayoutParams lp, IWindowManager wm, Context context)
        {
            _params = lp;
            _windowManager = wm;
            _context = context;
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
                    
                    if (diffX < MOVEMENT_THRESHOLD && diffY < MOVEMENT_THRESHOLD)
                    {
                        var service = IPlatformApplication.Current.Services.GetService<ITransformService>();
                        if (service != null)
                        {
                            if (service.IsTransformActive)
                                service.DisableTransformMode();
                            else
                                service.EnableTransformMode();
                        }
                    }
                    return true;
            }
            return false;
        }
    }
}
