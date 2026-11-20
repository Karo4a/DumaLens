using DumaLens.Api.Client;
using DumaLens.Web.Components;
using DumaLens.Web.Infrastructure;
using DumaLens.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

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
        builder.Services.AddAuthorizationCore();

        var apiBaseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl")
            ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured");

        // Аутентификация (Э9): токены в ProtectedLocalStorage, состояние пользователя на цепь,
        // Bearer + авто-refresh в обработчике HttpClient
        builder.Services.AddScoped<IClientTokenStore, ClientTokenStore>();
        builder.Services.AddScoped<UserState>();
        builder.Services.AddScoped<UserStateInitializer>();
        builder.Services.AddScoped<AuthenticationStateProvider, UserStateAuthenticationStateProvider>();
        // Один семафор обновления токенов на цепь: общие для всех NSwag-клиентов цепи
        builder.Services.AddScoped<RefreshLock>();

        // Клиент без обработчика аутентификации — для обновления токенов (без рекурсии)
        builder.Services.AddHttpClient("DumaLensApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        builder.Services.AddScoped<IAuthClient>(sp =>
            new AuthClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IUsersClient>(sp =>
            new UsersClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IVotesClient>(sp =>
            new VotesClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IBillsClient>(sp =>
            new BillsClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IFactionsClient>(sp =>
            new FactionsClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IDeputiesClient>(sp =>
            new DeputiesClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IConvocationsClient>(sp =>
            new ConvocationsClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));
        builder.Services.AddScoped<IStatsClient>(sp =>
            new StatsClient(apiBaseUrl, CreateAuthenticatedHttpClient(sp)));

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

    /// <summary>
    /// HttpClient NSwag-клиента с Bearer-токеном и авто-refresh при 401 (scoped на цепь)
    /// </summary>
    private static HttpClient CreateAuthenticatedHttpClient(IServiceProvider provider)
    {
        var handler = new AuthDelegatingHandler(
            provider.GetRequiredService<IClientTokenStore>(),
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<RefreshLock>())
        {
            InnerHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            },
        };
        return new HttpClient(handler, disposeHandler: false);
    }
}