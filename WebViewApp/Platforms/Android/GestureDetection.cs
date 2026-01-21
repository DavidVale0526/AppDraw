using Android.Content;
using Android.Views;
using Android.Widget;
using View = Android.Views.View;

namespace WebViewApp.Platforms.Android;

public class NativeGestureHandler
{
    private readonly Context _context;
    private readonly View _view;
    private readonly IDemoGestureListener _listener;
    private readonly ScaleGestureDetector _scaleDetector;
    private readonly RotationGestureDetector _rotationDetector;

    private float _scaleFactor = 1.0f;
    private float _rotationDegrees = 0f;
    private float _lastTouchX;
    private float _lastTouchY;
    private bool _isDragging;
    private int _activePointerId = InvalidPointerId;
    private const int InvalidPointerId = -1;

    public NativeGestureHandler(Context context, View view, IDemoGestureListener listener)
    {
        _context = context;
        _view = view;
        _listener = listener;
        _scaleDetector = new ScaleGestureDetector(context, new ScaleListener(this));
        _rotationDetector = new RotationGestureDetector(new RotationListener(this));
    }

    public bool OnTouch(MotionEvent? e)
    {
        if (e == null) return false;

        // Always process detectors so they track state (e.g. DOWN events)
        _scaleDetector.OnTouchEvent(e);
        _rotationDetector.OnTouchEvent(e);


        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
                if (e.PointerCount == 2)
                {
                    // Initialize two-finger drag
                    _lastTouchX = (e.GetX(0) + e.GetX(1)) / 2;
                    _lastTouchY = (e.GetY(0) + e.GetY(1)) / 2;
                    _isDragging = true;
                }
                else
                {
                    _isDragging = false;
                }
                break;

            case MotionEventActions.Move:
                if (e.PointerCount == 2 && _isDragging)
                {
                    // Calculate focal point (centroid) of the two fingers
                    float currentFocalX = (e.GetX(0) + e.GetX(1)) / 2;
                    float currentFocalY = (e.GetY(0) + e.GetY(1)) / 2;

                    float dx = currentFocalX - _lastTouchX;
                    float dy = currentFocalY - _lastTouchY;

                    // Apply drag using the movement of the center point
                    _listener.OnDrag(dx, dy);

                    _lastTouchX = currentFocalX;
                    _lastTouchY = currentFocalY;
                }
                break;

            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            case MotionEventActions.Cancel:
                if (e.PointerCount < 2)
                {
                    _isDragging = false;
                }
                // If one finger lifts but another remains, we stop dragging until 2 are down again
                // or we could fallback to 1, but user requested ONLY 2 fingers.
                break;
        }
        return true;
    }

    public void OnScale(float scaleFactor)
    {
        _listener.OnScale(scaleFactor);
    }

    public void OnRotate(float angle)
    {
        _listener.OnRotate(angle);
    }

    private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private readonly NativeGestureHandler _handler;

        public ScaleListener(NativeGestureHandler handler)
        {
            _handler = handler;
        }

        public override bool OnScale(ScaleGestureDetector? detector)
        {
            if (detector == null) return false;
            _handler.OnScale(detector.ScaleFactor);
            return true;
        }
    }

    private class RotationListener : RotationGestureDetector.IOnRotationGestureListener
    {
        private readonly NativeGestureHandler _handler;

        public RotationListener(NativeGestureHandler handler)
        {
            _handler = handler;
        }

        public void OnRotation(RotationGestureDetector detector)
        {
            _handler.OnRotate(-detector.Angle); // Invert angle for natural feel usually
        }
    }
}

public interface IDemoGestureListener
{
    void OnScale(float scaleFactor);
    void OnRotate(float angle);
    void OnDrag(float dx, float dy);
}

public class RotationGestureDetector
{
    public interface IOnRotationGestureListener
    {
        void OnRotation(RotationGestureDetector detector);
    }

    private readonly IOnRotationGestureListener _listener;
    private float _angle;
    private int _ptrID1, _ptrID2;
    private float _fX, _fY, _sX, _sY;

    public float Angle => _angle;

    public RotationGestureDetector(IOnRotationGestureListener listener)
    {
        _listener = listener;
        _ptrID1 = InvalidPointerId;
        _ptrID2 = InvalidPointerId;
    }

    private const int InvalidPointerId = -1;

    public bool OnTouchEvent(MotionEvent? e)
    {
        if (e == null) return false;

        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                _ptrID1 = e.GetPointerId(0);
                break;
            case MotionEventActions.PointerDown:
                if (_ptrID1 != InvalidPointerId)
                {
                    _ptrID2 = e.GetPointerId(e.ActionIndex);
                    int ptrIndex1 = e.FindPointerIndex(_ptrID1);
                    int ptrIndex2 = e.FindPointerIndex(_ptrID2);
                    
                    if (ptrIndex1 != -1 && ptrIndex2 != -1)
                    {
                        _sX = e.GetX(ptrIndex1);
                        _sY = e.GetY(ptrIndex1);
                        _fX = e.GetX(ptrIndex2);
                        _fY = e.GetY(ptrIndex2);
                    }
                }
                break;
            case MotionEventActions.Move:
                if (_ptrID1 != InvalidPointerId && _ptrID2 != InvalidPointerId)
                {
                    int ptrIndex1 = e.FindPointerIndex(_ptrID1);
                    int ptrIndex2 = e.FindPointerIndex(_ptrID2);

                    // Check if pointers are still valid
                    if (ptrIndex1 == -1 || ptrIndex2 == -1) return true;

                    float nsX = e.GetX(ptrIndex1);
                    float nsY = e.GetY(ptrIndex1);
                    float nfX = e.GetX(ptrIndex2);
                    float nfY = e.GetY(ptrIndex2);

                    _angle = AngleBetweenLines(_fX, _fY, _sX, _sY, nfX, nfY, nsX, nsY);

                    if (_listener != null)
                        _listener.OnRotation(this);

                    // Update points
                    _fX = nfX;
                    _fY = nfY;
                    _sX = nsX;
                    _sY = nsY;
                }
                break;
            case MotionEventActions.Up:
                _ptrID1 = InvalidPointerId;
                break;
            case MotionEventActions.PointerUp:
                _ptrID2 = InvalidPointerId;
                break;
            case MotionEventActions.Cancel:
                _ptrID1 = InvalidPointerId;
                _ptrID2 = InvalidPointerId;
                break;
        }
        return true;
    }

    private float AngleBetweenLines(float fX, float fY, float sX, float sY, float nfX, float nfY, float nsX, float nsY)
    {
        float angle1 = (float)Math.Atan2((fY - sY), (fX - sX));
        float angle2 = (float)Math.Atan2((nfY - nsY), (nfX - nsX));

        float angle = ((float)Math.Atan2(Math.Sin(angle1 - angle2), Math.Cos(angle1 - angle2)) * 180 / (float)Math.PI);
        return angle;
    }
}
