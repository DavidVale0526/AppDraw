using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Runtime;

namespace WebViewApp.Platforms.Android;

public class TransformService : ITransformService
{
    private bool _isTransformIconActive = false;
    private bool _isTransformActive = false;
    private global::Android.Views.View? _transformOverlay;
    private ImageView? _transformImageView;
    private IWindowManager? _windowManager;
    private WindowManagerLayoutParams? _overlayParams;
    
    // Transform state
    private float _scale = 1.0f;
    private float _rotation = 0f;
    private float _translationX = 0f;
    private float _translationY = 0f;

    public bool IsTransformActive => _isTransformActive;

    public void ToggleTransformIcon()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;

        if (_isTransformIconActive)
        {
            StopTransformIcon(activity);
            _isTransformIconActive = false;
        }
        else
        {
            if (!global::Android.Provider.Settings.CanDrawOverlays(activity))
            {
                RequestOverlayPermission(activity);
                return;
            }
            StartTransformIcon(activity);
            _isTransformIconActive = true;
        }
    }

    public event EventHandler<byte[]> ImageCaptured;

    public void EnableTransformMode()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return;

        activity.RunOnUiThread(() =>
        {
            try
            {
                // Get the WebView from MainPage
                var webView = GetWebViewFromMainPage();
                if (webView == null) return;

                // Capture WebView as bitmap
                var bitmap = CaptureWebView(webView);
                if (bitmap == null) return;

                // Compress bitmap to byte array
                using (var stream = new MemoryStream())
                {
                    bitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    var imageBytes = stream.ToArray();
                    
                    // Invoke event on main thread to update UI
                    ImageCaptured?.Invoke(this, imageBytes);
                }
                
                _isTransformActive = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enabling transform mode: {ex.Message}");
            }
        });
    }

    public void DisableTransformMode()
    {
        _isTransformActive = false;
        // No overlay to remove anymore
    }

    public void UpdateTransform(float scale, float rotation, float translationX, float translationY)
    {
        if (_transformImageView == null) return;

        _scale = scale;
        _rotation = rotation;
        _translationX = translationX;
        _translationY = translationY;

        var matrix = new Matrix();
        matrix.PostScale(scale, scale);
        matrix.PostRotate(rotation);
        matrix.PostTranslate(translationX, translationY);

        _transformImageView.ImageMatrix = matrix;
    }

    private void StartTransformIcon(Activity activity)
    {
        var intent = new Intent(activity, typeof(TransformIconService));
        activity.StartService(intent);
    }

    private void StopTransformIcon(Activity activity)
    {
        var intent = new Intent(activity, typeof(TransformIconService));
        activity.StopService(intent);
    }

    private void RequestOverlayPermission(Activity activity)
    {
        var intent = new Intent(global::Android.Provider.Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse("package:" + activity.PackageName));
        activity.StartActivity(intent);
    }

    private global::Android.Views.View? GetWebViewFromMainPage()
    {
        try
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) return null;

            // Navigate through the view hierarchy to find the WebView
            var rootView = activity.Window?.DecorView.FindViewById(global::Android.Resource.Id.Content);
            return FindWebViewInHierarchy(rootView);
        }
        catch
        {
            return null;
        }
    }

    private global::Android.Views.View? FindWebViewInHierarchy(global::Android.Views.View? view)
    {
        if (view == null) return null;

        if (view is global::Android.Webkit.WebView webView)
            return webView;

        if (view is ViewGroup viewGroup)
        {
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                var result = FindWebViewInHierarchy(viewGroup.GetChildAt(i));
                if (result != null) return result;
            }
        }

        return null;
    }

    private Bitmap? CaptureWebView(global::Android.Views.View view)
    {
        try
        {
            view.DrawingCacheEnabled = true;
            view.BuildDrawingCache();
            var bitmap = Bitmap.CreateBitmap(view.DrawingCache);
            view.DrawingCacheEnabled = false;
            return bitmap;
        }
        catch
        {
            // Fallback: Create bitmap manually
            try
            {
                var bitmap = Bitmap.CreateBitmap(view.Width, view.Height, Bitmap.Config.Argb8888);
                var canvas = new Canvas(bitmap);
                view.Draw(canvas);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }

    private class TransformGestureDetector
    {
        private readonly Context _context;
        private readonly TransformService _service;
        private readonly ImageView _imageView;
        private readonly ScaleGestureDetector _scaleDetector;
        private readonly RotationGestureDetector _rotationDetector;

        private float _scaleFactor = 1.0f;
        private float _rotationDegrees = 0f;
        private float _translationX = 0f;
        private float _translationY = 0f;
        
        private float _lastTouchX = 0f;
        private float _lastTouchY = 0f;
        private bool _isDragging = false;

        public TransformGestureDetector(Context context, TransformService service, ImageView imageView)
        {
            _context = context;
            _service = service;
            _imageView = imageView;
            
            _scaleDetector = new ScaleGestureDetector(context, new ScaleListener(this));
            _rotationDetector = new RotationGestureDetector(new RotationListener(this));
        }

        public void OnTouch(MotionEvent? e)
        {
            if (e == null) return;

            _scaleDetector.OnTouchEvent(e);
            _rotationDetector.OnTouchEvent(e);

            int pointerCount = e.PointerCount;

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    _lastTouchX = e.GetX();
                    _lastTouchY = e.GetY();
                    _isDragging = false;
                    break;

                case MotionEventActions.PointerDown:
                    _isDragging = false;
                    break;

                case MotionEventActions.Move:
                    if (pointerCount == 1 && !_scaleDetector.IsInProgress)
                    {
                        // Single finger pan
                        float dx = e.GetX() - _lastTouchX;
                        float dy = e.GetY() - _lastTouchY;
                        
                        if (Math.Abs(dx) > 5 || Math.Abs(dy) > 5)
                        {
                            _isDragging = true;
                            _translationX += dx;
                            _translationY += dy;
                            UpdateMatrix();
                        }
                        
                        _lastTouchX = e.GetX();
                        _lastTouchY = e.GetY();
                    }
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                    if (!_isDragging && pointerCount == 1)
                    {
                        // Double tap to reset
                        // Could implement gesture detector here
                    }
                    break;
            }
        }

        private void UpdateMatrix()
        {
            _service.UpdateTransform(_scaleFactor, _rotationDegrees, _translationX, _translationY);
        }

        private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            private readonly TransformGestureDetector _parent;

            public ScaleListener(TransformGestureDetector parent)
            {
                _parent = parent;
            }

            public override bool OnScale(ScaleGestureDetector? detector)
            {
                if (detector == null) return false;

                _parent._scaleFactor *= detector.ScaleFactor;
                _parent._scaleFactor = Math.Max(0.1f, Math.Min(_parent._scaleFactor, 10.0f));
                _parent.UpdateMatrix();
                return true;
            }
        }

        private class RotationListener : RotationGestureDetector.IOnRotationGestureListener
        {
            private readonly TransformGestureDetector _parent;

            public RotationListener(TransformGestureDetector parent)
            {
                _parent = parent;
            }

            public void OnRotation(RotationGestureDetector detector)
            {
                _parent._rotationDegrees -= detector.Angle;
                _parent.UpdateMatrix();
            }
        }
    }

    // Simple rotation gesture detector
    private class RotationGestureDetector
    {
        public interface IOnRotationGestureListener
        {
            void OnRotation(RotationGestureDetector detector);
        }

        private readonly IOnRotationGestureListener _listener;
        private float _angle = 0f;
        private float _previousAngle = 0f;

        public float Angle => _angle;

        public RotationGestureDetector(IOnRotationGestureListener listener)
        {
            _listener = listener;
        }

        public void OnTouchEvent(MotionEvent? e)
        {
            if (e == null || e.PointerCount < 2) return;

            float deltaX = e.GetX(1) - e.GetX(0);
            float deltaY = e.GetY(1) - e.GetY(0);
            float currentAngle = (float)(Math.Atan2(deltaY, deltaX) * 180 / Math.PI);

            switch (e.ActionMasked)
            {
                case MotionEventActions.PointerDown:
                    _previousAngle = currentAngle;
                    break;

                case MotionEventActions.Move:
                    _angle = currentAngle - _previousAngle;
                    _listener.OnRotation(this);
                    _previousAngle = currentAngle;
                    break;
            }
        }
    }
}
