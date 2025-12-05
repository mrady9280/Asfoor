using Asfoor.Web.Components;
using Asfoor.Shared.Services;
using Asfoor.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddServiceDefaults();
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add device-specific services used by the Asfoor.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddHttpClient<IAiService, AiApiService>(client =>
    {
        client.BaseAddress = new("https+http://api");
        client.Timeout = TimeSpan.FromMinutes(15);
    }
);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(Asfoor.Shared._Imports).Assembly);
app.MapDefaultEndpoints();


app.Run();