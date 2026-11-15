using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис сводных показателей базы
/// </summary>
public interface IStatsOverviewService
{
    /// <summary>
    /// Получает сводку для главной страницы
    /// </summary>
    Task<StatsOverviewModel> GetOverview(CancellationToken cancellationToken);
}