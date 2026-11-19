using DumaLens.Api.Client;
using DumaLens.Web.Components;

namespace DumaLens.Web;

/// <summary>
/// Класс программы
/// </summary>
public partial class Program
{
    /// <summary>
    /// Точка входа программы
    /// </summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var apiBaseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl")
            ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured");

        builder.Services.AddHttpClient("DumaLensApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        builder.Services.AddScoped<IVotesClient>(sp =>
            new VotesClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient("DumaLensApi")));
        builder.Services.AddScoped<IBillsClient>(sp =>
            new BillsClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient("DumaLensApi")));
        builder.Services.AddScoped<IFactionsClient>(sp =>
            new FactionsClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient("DumaLensApi")));
        builder.Services.AddScoped<IDeputiesClient>(sp =>
            new DeputiesClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient("DumaLensApi")));
        builder.Services.AddScoped<IConvocationsClient>(sp =>
            new ConvocationsClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient("DumaLensApi")));
        builder.Services.AddScoped<IStatsClient>(sp =>
            new StatsClient(apiBaseUrl, sp.GetRequiredService<IHttpClientFactory>().CreateClient("DumaLensApi")));

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        // Редирект на HTTPS включается только при настроенном https-эндпоинте:
        // демо-комплект и http-профиль работают по HTTP, иначе middleware пишет
        // предупреждение "Failed to determine the https port for redirect"
        var urls = app.Configuration["ASPNETCORE_URLS"] ?? app.Configuration["urls"];
        if (urls is not null && urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.Run();
    }
}