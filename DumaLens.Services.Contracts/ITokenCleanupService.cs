namespace DumaLens.Services.Contracts;

/// <summary>
/// Очистка утративших силу refresh-токенов (фоновое обслуживание Api)
/// </summary>
public interface ITokenCleanupService
{
    /// <summary>
    /// Удаляет истёкшие и отозванные refresh-токены старше срока хранения; возвращает число удалённых записей
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    Task<int> CleanupAsync(CancellationToken cancellationToken);
}