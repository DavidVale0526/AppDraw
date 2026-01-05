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

        private void OnGhostModeClicked(object? sender, EventArgs e)
        {
            _ghostModeService.EnableGhostMode();
        }
    }
}
