using Microsoft.Extensions.Logging;
using Asfoor.Shared.Services;
using Asfoor.Services;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Microsoft.Extensions.Configuration;

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

        builder.Services.AddHttpClient<IAiService, AiApiService>((serviceProvider, client) =>
        {
            string baseUrl = "http://localhost:5062";
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                baseUrl = "http://10.0.2.2:5062";
            }

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMinutes(15);
        });

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