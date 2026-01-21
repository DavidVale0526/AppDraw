using Microsoft.Extensions.Logging;

namespace WebViewApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddTransient<MainPage>();

#if ANDROID
		builder.Services.AddSingleton<IGhostModeService, WebViewApp.Platforms.Android.GhostModeService>();
		builder.Services.AddSingleton<ITransformService, WebViewApp.Platforms.Android.TransformService>();
#else
		builder.Services.AddSingleton<IGhostModeService, DummyGhostModeService>();
		builder.Services.AddSingleton<ITransformService, DummyTransformService>();
#endif

            return builder.Build();
        }
    }
}
