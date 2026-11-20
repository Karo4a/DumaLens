using DumaLens.Services.Contracts;

namespace DumaLens.Api.Infrastructure;

/// <summary>
/// Фоновая очистка утративших силу refresh-токенов: суточный таймер (Auth:TokenCleanupIntervalHours),
/// срок хранения задаётся Auth:RefreshTokenRetentionDays. Ошибка очистки не роняет хост — логируется и повторяется в следующем тике
/// </summary>
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly AuthOptions options;
    private readonly ILogger<TokenCleanupBackgroundService> logger;

    /// <summary>
    /// Конструктор
    /// </summary>
    public TokenCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        AuthOptions options,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options;
        this.logger = logger;
    }

    /// <inheritdoc cref="BackgroundService.ExecuteAsync"/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.TokenCleanupIntervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<ITokenCleanupService>();
                var deleted = await cleanupService.CleanupAsync(stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Очистка refresh-токенов: удалено {Count} записей", deleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Очистка refresh-токенов не выполнена");
            }
        }
    }
}