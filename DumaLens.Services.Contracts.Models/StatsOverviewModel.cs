namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Сводные показатели базы для главной страницы
/// </summary>
public class StatsOverviewModel
{
    /// <summary>
    /// Число голосований
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Число депутатов
    /// </summary>
    public int TotalDeputies { get; set; }

    /// <summary>
    /// Число канонических фракций
    /// </summary>
    public int TotalFactions { get; set; }

    /// <summary>
    /// Число законопроектов
    /// </summary>
    public int TotalBills { get; set; }

    /// <summary>
    /// Дата самого раннего голосования по московскому времени (null — голосований нет)
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Дата самого позднего голосования по московскому времени (null — голосований нет)
    /// </summary>
    public DateOnly? DateTo { get; set; }
}