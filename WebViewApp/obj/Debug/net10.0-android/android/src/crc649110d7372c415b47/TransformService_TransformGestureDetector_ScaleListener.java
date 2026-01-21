package crc649110d7372c415b47;


public class TransformService_TransformGestureDetector_ScaleListener
	extends android.view.ScaleGestureDetector.SimpleOnScaleGestureListener
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onScale:(Landroid/view/ScaleGestureDetector;)Z:GetOnScale_Landroid_view_ScaleGestureDetector_Handler\n" +
			"";
		mono.android.Runtime.register ("WebViewApp.Platforms.Android.TransformService+TransformGestureDetector+ScaleListener, WebViewApp", TransformService_TransformGestureDetector_ScaleListener.class, __md_methods);
	}

	public TransformService_TransformGestureDetector_ScaleListener ()
	{
		super ();
		if (getClass () == TransformService_TransformGestureDetector_ScaleListener.class) {
			mono.android.TypeManager.Activate ("WebViewApp.Platforms.Android.TransformService+TransformGestureDetector+ScaleListener, WebViewApp", "", this, new java.lang.Object[] {  });
		}
	}

	public boolean onScale (android.view.ScaleGestureDetector p0)
	{
		return n_onScale (p0);
	}

	private native boolean n_onScale (android.view.ScaleGestureDetector p0);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
