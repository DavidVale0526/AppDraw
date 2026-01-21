using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace WebViewApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IGhostModeService _ghostModeService;
        public ObservableCollection<WebTab> Tabs { get; } = new();
        public ObservableCollection<Favorite> Favorites { get; } = new();
        private WebTab? _activeTab;
        private bool _isDesktopMode = false;
        private const string DesktopUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        private string? _defaultUserAgent;

        // Image Transformation Fields
        private double _currentScale = 1;
        private double _startScale = 1;
        private double _xOffset = 0;
        private double _yOffset = 0;

        public MainPage(IGhostModeService ghostModeService)
        {
            InitializeComponent();
            _ghostModeService = ghostModeService;
            
            // Restore opacity preference
            float savedOpacity = Preferences.Default.Get("GhostModeOpacity", 0.5f);
            _ghostModeService.Opacity = savedOpacity;
            OpacitySlider.Value = savedOpacity;

            BindingContext = this;
            
            TabsCollection.ItemsSource = Tabs;
            FavoritesCollection.ItemsSource = Favorites;
            
            LoadFavorites();
            AddNewTab("https://www.google.com");
        }

        private void OnToggleGhostModeClicked(object? sender, EventArgs e)
        {
            _ghostModeService.ToggleGhostMode();
        }

        private void OnShowFloatingIconClicked(object? sender, EventArgs e)
        {
            _ghostModeService.ToggleFloatingIcon();
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null)
                {
                    OnImageDoubleTapped(this, EventArgs.Empty);
                    OverlayImage.Source = ImageSource.FromFile(photo.FullPath);
                    ImageContainer.IsVisible = true;
                    MainWebView.IsVisible = false;
                    UrlEntry.Text = "🖼️ Modo Imagen: " + photo.FileName;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar la imagen: " + ex.Message, "OK");
            }
        }

        private void OnImageTapped(object sender, EventArgs e)
        {
            ImageContainer.IsVisible = false;
            MainWebView.IsVisible = true;
            if (_activeTab != null) UrlEntry.Text = _activeTab.Url;
        }

        private void OnImageDoubleTapped(object sender, EventArgs e)
        {
            _currentScale = 1;
            _xOffset = 0;
            _yOffset = 0;
            OverlayImage.Scale = 1;
            OverlayImage.TranslationX = 0;
            OverlayImage.TranslationY = 0;
            OverlayImage.Rotation = 0;
        }

        private void OnRotateImageClicked(object sender, EventArgs e)
        {
            OverlayImage.Rotation = (OverlayImage.Rotation + 90) % 360;
        }

        private void OnZoomInClicked(object sender, EventArgs e)
        {
            _currentScale = Math.Min(OverlayImage.Scale + 0.2, 10);
            OverlayImage.Scale = _currentScale;
        }

        private void OnZoomOutClicked(object sender, EventArgs e)
        {
            _currentScale = Math.Max(OverlayImage.Scale - 0.2, 0.5);
            OverlayImage.Scale = _currentScale;
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Confirm starting points
                    break;
                case GestureStatus.Running:
                    // TotalX is the cumulative distance since gesture start
                    double newX = _xOffset + e.TotalX;
                    double newY = _yOffset + e.TotalY;

                    // Get screen bounds for safety clamping
                    double screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                    double screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

                    // Limit translation so the image stays somewhat visible (within 1.5 screen sizes)
                    OverlayImage.TranslationX = Math.Clamp(newX, -screenWidth * 1.5, screenWidth * 1.5);
                    OverlayImage.TranslationY = Math.Clamp(newY, -screenHeight * 1.5, screenHeight * 1.5);
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    // Freeze the position for the next movement start
                    _xOffset = OverlayImage.TranslationX;
                    _yOffset = OverlayImage.TranslationY;
                    break;
            }
        }

        #region Favorites System

        private void OnAddFavoriteClicked(object sender, EventArgs e)
        {
            if (_activeTab == null || string.IsNullOrWhiteSpace(_activeTab.Url)) return;

            var currentUrl = _activeTab.Url;
            if (Favorites.Any(f => f.Url == currentUrl)) return;

            var favorite = new Favorite
            {
                Url = currentUrl,
                Title = _activeTab.Title ?? "Favorito",
                Icon = GetEmojiForUrl(currentUrl)
            };

            Favorites.Add(favorite);
            SaveFavorites();
        }

        private void OnFavoriteSelected(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Favorite favorite)
            {
                var url = favorite.Url;
                if (string.IsNullOrWhiteSpace(url)) return;

                // Sync URL entry and navigate
                UrlEntry.Text = url;
                if (_activeTab != null)
                {
                    _activeTab.Url = url;
                    _activeTab.Title = favorite.Title;
                }
                
                MainWebView.Source = url;
            }
        }

        private void OnRemoveFavoriteClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Favorite favorite)
            {
                Favorites.Remove(favorite);
                SaveFavorites();
            }
        }

        private void LoadFavorites()
        {
            try
            {
                var json = Preferences.Get("UserFavorites", "[]");
                var list = JsonSerializer.Deserialize<List<Favorite>>(json);
                if (list != null)
                {
                    foreach (var fav in list) Favorites.Add(fav);
                }
            }
            catch { }
        }

        private void SaveFavorites()
        {
            try
            {
                var json = JsonSerializer.Serialize(Favorites.ToList());
                Preferences.Set("UserFavorites", json);
            }
            catch { }
        }

        private string GetEmojiForUrl(string url)
        {
            if (url.Contains("pinterest")) return "📌";
            if (url.Contains("google")) return "🔍";
            if (url.Contains("youtube")) return "🎥";
            if (url.Contains("facebook")) return "👥";
            if (url.Contains("twitter") || url.Contains("x.com")) return "🐦";
            if (url.Contains("instagram")) return "📸";
            if (url.Contains("github")) return "💻";
            return "⭐";
        }

        #endregion

        private void OnToggleDesktopModeClicked(object sender, EventArgs e)
        {
            _isDesktopMode = !_isDesktopMode;
            DesktopModeBtn.Text = _isDesktopMode ? "💻" : "📱";

#if ANDROID
            var handler = MainWebView.Handler;
            if (handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                if (_defaultUserAgent == null) _defaultUserAgent = nativeWebView.Settings.UserAgentString;
                nativeWebView.Settings.UserAgentString = _isDesktopMode ? DesktopUserAgent : _defaultUserAgent;
                nativeWebView.Settings.UseWideViewPort = _isDesktopMode;
                nativeWebView.Settings.LoadWithOverviewMode = _isDesktopMode;
            }
#endif
            MainWebView.Reload();
        }

        private void AddNewTab(string url)
        {
            foreach (var tab in Tabs) tab.IsSelected = false;

            var newTab = new WebTab { Url = url, IsSelected = true };
            Tabs.Add(newTab);
            _activeTab = newTab;
            
            MainWebView.Source = newTab.Url;
            TabsCollection.SelectedItem = newTab;
            UrlEntry.Text = newTab.Url;
        }

        private void OnNewTabClicked(object sender, EventArgs e) => AddNewTab("https://www.google.com");

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is WebTab selectedTab)
            {
                SwitchToTab(selectedTab);
            }
        }

        private void SwitchToTab(WebTab tab)
        {
            if (_activeTab != null) _activeTab.IsSelected = false;
            
            _activeTab = tab;
            _activeTab.IsSelected = true;
            
            ImageContainer.IsVisible = false;
            MainWebView.IsVisible = true;
            MainWebView.Source = _activeTab.Url;
            UrlEntry.Text = _activeTab.Url;
        }

        private void OnCloseTabClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is WebTab tabToRemove)
            {
                Tabs.Remove(tabToRemove);
                if (Tabs.Count == 0)
                {
                    AddNewTab("https://www.google.com");
                }
                else if (tabToRemove == _activeTab)
                {
                    SwitchToTab(Tabs.Last());
                }
            }
        }

        private void OnBackClicked(object sender, EventArgs e) { if (MainWebView.CanGoBack) MainWebView.GoBack(); }
        private void OnForwardClicked(object sender, EventArgs e) { if (MainWebView.CanGoForward) MainWebView.GoForward(); }
        private void OnRefreshClicked(object sender, EventArgs e) => MainWebView.Reload();

        private void OnUrlCompleted(object sender, EventArgs e)
        {
            var url = UrlEntry.Text;
            if (!url.StartsWith("http")) url = "https://" + url;
            MainWebView.Source = url;
        }

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            UrlEntry.Text = e.Url;
        }

        private async void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (_activeTab != null)
            {
                _activeTab.Url = e.Url;
                try
                {
                    // Intentar obtener el título real de la página usando JavaScript
                    string title = await MainWebView.EvaluateJavaScriptAsync("document.title");
                    if (!string.IsNullOrWhiteSpace(title) && title != "null")
                    {
                        _activeTab.Title = title;
                    }
                    else
                    {
                        _activeTab.Title = e.Url.Length > 20 ? e.Url.Substring(0, 20) + "..." : e.Url;
                    }
                }
                catch
                {
                    _activeTab.Title = e.Url.Length > 20 ? e.Url.Substring(0, 20) + "..." : e.Url;
                }
            }
        }

        private void OnToggleUiClicked(object sender, EventArgs e)
        {
            TopLevelUi.IsVisible = !TopLevelUi.IsVisible;
            
            // Opcional: Cambiar la opacidad o rotación del botón para indicar estado
            if (sender is ImageButton btn)
            {
                btn.Opacity = TopLevelUi.IsVisible ? 1.0 : 0.6;
                btn.Rotation = TopLevelUi.IsVisible ? 0 : 180;
            }
        }

        private void OnOpacityValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (_ghostModeService != null)
            {
                _ghostModeService.Opacity = (float)e.NewValue;
                Preferences.Default.Set("GhostModeOpacity", (float)e.NewValue);
            }
        }
    }
}
