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
        _floatingView.SetOnTouchListener(new FloatingTouchListener(_params, _windowManager!, this));

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
        private readonly Context _context;
        private int _initialX;
        private int _initialY;
        private float _initialTouchX;
        private float _initialTouchY;
        private long _touchDownTime;
        private bool _isDragging = false;
        private bool _isAdjustingOpacity = false;
        private float _initialOpacity;
        private bool _isLongPress;
        
        // Visual feedback overlay
        private global::Android.Views.View? _feedbackView;
        private TextView? _feedbackText;
        private WindowManagerLayoutParams? _feedbackParams;
        
        private const int LONG_PRESS_THRESHOLD = 500; // ms
        private const int MOVEMENT_THRESHOLD = 15; // pixels
        private const float OPACITY_SENSITIVITY = 0.003f; // Sensitivity for vertical swipe

        public FloatingTouchListener(WindowManagerLayoutParams lp, IWindowManager wm, Context context)
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
                    _touchDownTime = SystemClock.ElapsedRealtime();
                    _isDragging = false;
                    _isAdjustingOpacity = false;
                    
                    // Get current opacity
                    var service = IPlatformApplication.Current.Services.GetService<IGhostModeService>();
                    _initialOpacity = service?.Opacity ?? 0.5f;
                    return true;

                case MotionEventActions.Move:
                    float deltaX = e.RawX - _initialTouchX;
                    float deltaY = e.RawY - _initialTouchY;
                    float absDeltaX = Math.Abs(deltaX);
                    float absDeltaY = Math.Abs(deltaY);
                    long pressDuration = SystemClock.ElapsedRealtime() - _touchDownTime;

                    // Determine gesture type
                    if (!_isDragging && !_isAdjustingOpacity && (absDeltaX > MOVEMENT_THRESHOLD || absDeltaY > MOVEMENT_THRESHOLD))
                    {
                        if (pressDuration > LONG_PRESS_THRESHOLD)
                        {
                            // Long press detected - enable dragging
                            _isDragging = true;
                        }
                        else if (absDeltaY > absDeltaX * 1.5f) // Vertical movement dominant
                        {
                            // Vertical swipe - adjust opacity
                            _isAdjustingOpacity = true;
                            ShowFeedbackOverlay();
                        }
                        else if (absDeltaX > absDeltaY * 1.5f) // Horizontal movement dominant
                        {
                            // Horizontal swipe without long press - do nothing (or could enable quick drag)
                            return true;
                        }
                    }

                    if (_isDragging)
                    {
                        // Move the icon
                        _params.X = _initialX + (int)deltaX;
                        _params.Y = _initialY + (int)deltaY;
                        _windowManager.UpdateViewLayout(v, _params);
                    }
                    else if (_isAdjustingOpacity)
                    {
                        // Adjust opacity based on vertical movement
                        // Down = more transparent, Up = more opaque
                        float opacityChange = -deltaY * OPACITY_SENSITIVITY;
                        float newOpacity = Math.Clamp(_initialOpacity + opacityChange, 0.1f, 1.0f);
                        
                        var ghostService = IPlatformApplication.Current.Services.GetService<IGhostModeService>();
                        if (ghostService != null)
                        {
                            ghostService.Opacity = newOpacity;
                            UpdateFeedbackText(newOpacity);
                        }
                    }
                    return true;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    float diffX = Math.Abs(e.RawX - _initialTouchX);
                    float diffY = Math.Abs(e.RawY - _initialTouchY);
                    
                    // Hide feedback overlay
                    HideFeedbackOverlay();
                    
                    // If it was a tap (minimal movement and no long press)
                    if (!_isDragging && !_isAdjustingOpacity && diffX < MOVEMENT_THRESHOLD && diffY < MOVEMENT_THRESHOLD)
                    {
                        var toggleService = IPlatformApplication.Current.Services.GetService<IGhostModeService>();
                        toggleService?.ToggleGhostMode();
                    }
                    
                    // Save opacity preference if it was adjusted
                    if (_isAdjustingOpacity)
                    {
                        var ghostService = IPlatformApplication.Current.Services.GetService<IGhostModeService>();
                        if (ghostService != null)
                        {
                            Microsoft.Maui.Storage.Preferences.Default.Set("GhostModeOpacity", ghostService.Opacity);
                        }
                    }
                    
                    _isLongPress = false;
                    _isDragging = false;
                    _isAdjustingOpacity = false;
                    return true;
            }
            return false;
        }
        
        private void ShowFeedbackOverlay()
        {
            if (_feedbackView != null) return;
            
            try
            {
                // Create feedback text view
                _feedbackText = new TextView(_context)
                {
                    TextSize = 16,
                    Gravity = GravityFlags.Center
                };
                _feedbackText.SetTextColor(global::Android.Graphics.Color.White);
                _feedbackText.SetBackgroundColor(global::Android.Graphics.Color.Argb(200, 0, 0, 0));
                _feedbackText.SetPadding(20, 10, 20, 10);
                
                var type = Build.VERSION.SdkInt >= BuildVersionCodes.O 
                    ? WindowManagerTypes.ApplicationOverlay 
                    : WindowManagerTypes.Phone;
                
                _feedbackParams = new WindowManagerLayoutParams(
                    WindowManagerLayoutParams.WrapContent,
                    WindowManagerLayoutParams.WrapContent,
                    type,
                    WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchable,
                    Format.Translucent)
                {
                    Gravity = GravityFlags.Center,
                    X = 0,
                    Y = 0
                };
                
                _feedbackView = _feedbackText;
                _windowManager.AddView(_feedbackView, _feedbackParams);
            }
            catch
            {
                // Silently fail if overlay can't be created
            }
        }
        
        private void UpdateFeedbackText(float opacity)
        {
            if (_feedbackText != null)
            {
                int percentage = (int)(opacity * 100);
                _feedbackText.Text = $"👻 {percentage}%";
            }
        }
        
        private void HideFeedbackOverlay()
        {
            if (_feedbackView != null)
            {
                try
                {
                    _windowManager.RemoveView(_feedbackView);
                }
                catch
                {
                    // Silently fail
                }
                _feedbackView = null;
                _feedbackText = null;
                _feedbackParams = null;
            }
        }
    }
}
