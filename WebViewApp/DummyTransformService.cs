namespace WebViewApp;

public class DummyTransformService : ITransformService
{
    public void ToggleTransformIcon() { }
    public void EnableTransformMode() { }
    public void DisableTransformMode() { }
    public bool IsTransformActive => false;
    public event EventHandler<byte[]> ImageCaptured;
}
