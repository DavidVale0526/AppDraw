package crc649110d7372c415b47;


public class FloatingIconService_FloatingTouchListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		android.view.View.OnTouchListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onTouch:(Landroid/view/View;Landroid/view/MotionEvent;)Z:GetOnTouch_Landroid_view_View_Landroid_view_MotionEvent_Handler:Android.Views.View+IOnTouchListenerInvoker, Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\n" +
			"";
		mono.android.Runtime.register ("WebViewApp.Platforms.Android.FloatingIconService+FloatingTouchListener, WebViewApp", FloatingIconService_FloatingTouchListener.class, __md_methods);
	}

	public FloatingIconService_FloatingTouchListener ()
	{
		super ();
		if (getClass () == FloatingIconService_FloatingTouchListener.class) {
			mono.android.TypeManager.Activate ("WebViewApp.Platforms.Android.FloatingIconService+FloatingTouchListener, WebViewApp", "", this, new java.lang.Object[] {  });
		}
	}

	public FloatingIconService_FloatingTouchListener (android.view.WindowManager.LayoutParams p0, android.view.WindowManager p1, android.content.Context p2)
	{
		super ();
		if (getClass () == FloatingIconService_FloatingTouchListener.class) {
			mono.android.TypeManager.Activate ("WebViewApp.Platforms.Android.FloatingIconService+FloatingTouchListener, WebViewApp", "Android.Views.WindowManagerLayoutParams, Mono.Android:Android.Views.IWindowManager, Mono.Android:Android.Content.Context, Mono.Android", this, new java.lang.Object[] { p0, p1, p2 });
		}
	}

	public boolean onTouch (android.view.View p0, android.view.MotionEvent p1)
	{
		return n_onTouch (p0, p1);
	}

	private native boolean n_onTouch (android.view.View p0, android.view.MotionEvent p1);

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
