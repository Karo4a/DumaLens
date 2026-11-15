namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Сводные показатели базы для главной страницы
/// </summary>
public class StatsOverviewDbModel
{
    /// <summary>
    /// Число голосований
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Число депутатов
    /// </summary>
    public int DeputyCount { get; set; }

    /// <summary>
    /// Число канонических фракций
    /// </summary>
    public int FactionCount { get; set; }

    /// <summary>
    /// Число законопроектов
    /// </summary>
    public int BillCount { get; set; }

    /// <summary>
    /// Московское время самого раннего голосования (null — голосований нет)
    /// </summary>
    public DateTimeOffset? DateFrom { get; set; }

    /// <summary>
    /// Московское время самого позднего голосования (null — голосований нет)
    /// </summary>
    public DateTimeOffset? DateTo { get; set; }
}