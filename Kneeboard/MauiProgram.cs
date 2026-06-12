using CommunityToolkit.Maui;
using Kneeboard.Services;
using Kneeboard.ViewModels;
using Kneeboard.Views;
using Microsoft.Extensions.Logging;

namespace Kneeboard;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IDocumentService, DocumentService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

#if WINDOWS
        builder.Services.AddSingleton<IPdfService, Platforms.Windows.PdfService>();
#elif ANDROID
        builder.Services.AddSingleton<IPdfService, Platforms.Android.PdfService>();
#endif

        // Pages + ViewModels (transient: fresh instance each navigation)
        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<KneeboardViewModel>();
        builder.Services.AddTransient<KneeboardPage>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }
}
