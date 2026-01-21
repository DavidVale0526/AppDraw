package crc649110d7372c415b47;


public class FloatingIconService
	extends android.app.Service
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onBind:(Landroid/content/Intent;)Landroid/os/IBinder;:GetOnBind_Landroid_content_Intent_Handler\n" +
			"n_onCreate:()V:GetOnCreateHandler\n" +
			"n_onDestroy:()V:GetOnDestroyHandler\n" +
			"";
		mono.android.Runtime.register ("WebViewApp.Platforms.Android.FloatingIconService, WebViewApp", FloatingIconService.class, __md_methods);
	}

	public FloatingIconService ()
	{
		super ();
		if (getClass () == FloatingIconService.class) {
			mono.android.TypeManager.Activate ("WebViewApp.Platforms.Android.FloatingIconService, WebViewApp", "", this, new java.lang.Object[] {  });
		}
	}

	public android.os.IBinder onBind (android.content.Intent p0)
	{
		return n_onBind (p0);
	}

	private native android.os.IBinder n_onBind (android.content.Intent p0);

	public void onCreate ()
	{
		n_onCreate ();
	}

	private native void n_onCreate ();

	public void onDestroy ()
	{
		n_onDestroy ();
	}

	private native void n_onDestroy ();

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
