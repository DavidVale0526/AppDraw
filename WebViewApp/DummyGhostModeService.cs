namespace WebViewApp;

public class DummyGhostModeService : IGhostModeService
{
    public Task EnableGhostMode() => Task.CompletedTask;
    public void DisableGhostMode() { }
}
