using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения сводных показателей базы
/// </summary>
public interface IStatsReadRepository
{
    /// <summary>
    /// Получает сводные показатели: счётчики сущностей и диапазон дат голосований
    /// </summary>
    Task<StatsOverviewDbModel> GetOverview(CancellationToken cancellationToken);
}