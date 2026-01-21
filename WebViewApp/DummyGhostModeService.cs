namespace WebViewApp;

public class DummyGhostModeService : IGhostModeService
{
    public Task EnableGhostMode() => Task.CompletedTask;
    public void DisableGhostMode() { }
    public void ToggleGhostMode() { }
    public void RequestOverlayPermission() { }
    public void ToggleFloatingIcon() { }
    public float Opacity { get; set; } = 0.5f;
}
