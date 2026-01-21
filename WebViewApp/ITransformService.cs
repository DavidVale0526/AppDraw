namespace WebViewApp;

public interface ITransformService
{
    void ToggleTransformIcon();
    void EnableTransformMode();
    void DisableTransformMode();
    bool IsTransformActive { get; }
    event EventHandler<byte[]> ImageCaptured;
}
