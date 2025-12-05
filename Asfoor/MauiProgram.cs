using Microsoft.Extensions.Logging;
using Asfoor.Shared.Services;
using Asfoor.Services;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;

namespace Asfoor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.AddServiceDefaults();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddHttpClient<IAiService, AiApiService>(client =>
            {
                client.BaseAddress = new("https+http://api");
                client.Timeout = TimeSpan.FromMinutes(15);
            }
        );

        // Add device-specific services used by the Asfoor.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();
        // Add MudBlazor services
        builder.Services.AddMudServices();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}