namespace WebViewApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IGhostModeService _ghostModeService;

        public MainPage(IGhostModeService ghostModeService)
        {
            InitializeComponent();
            _ghostModeService = ghostModeService;
        }

        private async void OnGhostModeClicked(object? sender, EventArgs e)
        {
            await _ghostModeService.EnableGhostMode();
        }
    }
}
