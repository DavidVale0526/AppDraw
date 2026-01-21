namespace WebViewApp;

public interface IGhostModeService
{
    Task EnableGhostMode();
    void DisableGhostMode();
    void ToggleGhostMode();
    void RequestOverlayPermission();
    void ToggleFloatingIcon();
    float Opacity { get; set; }
}
