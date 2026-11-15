using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Строка выдачи поиска голосований
/// </summary>
public class VoteSearchDbModel
{
    /// <summary>
    /// Идентификатор голосования
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор голосования на сайте голосований
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Название (предмет) голосования
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время голосования в UTC (как хранится в базе)
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

    /// <summary>
    /// Историческое смещение московского времени на момент голосования, минуты
    /// </summary>
    public int UtcOffsetMinutes { get; set; }

    /// <summary>
    /// Номер созыва
    /// </summary>
    public int ConvocationNumber { get; set; }

    /// <summary>
    /// Номер законопроекта (null, если голосование вне законопроекта)
    /// </summary>
    public string? BillNumber { get; set; }

    /// <summary>
    /// Стадия голосования
    /// </summary>
    public VoteStage Stage { get; set; }

    /// <summary>
    /// Исход голосования
    /// </summary>
    public VoteOutcome Outcome { get; set; }

    /// <summary>
    /// Число голосов «за»
    /// </summary>
    public int AcceptCount { get; set; }

    /// <summary>
    /// Число голосов «против»
    /// </summary>
    public int DeclineCount { get; set; }

    /// <summary>
    /// Число голосов «воздержался»
    /// </summary>
    public int AbstainCount { get; set; }

    /// <summary>
    /// Число не голосовавших депутатов
    /// </summary>
    public int NoneCount { get; set; }

    /// <summary>
    /// Наличие результатов голосования
    /// </summary>
    public bool HasResults { get; set; }

    /// <summary>
    /// Фракционные строки голосования (по коду фракции)
    /// </summary>
    public IReadOnlyCollection<VoteFactionStatsDbModel> FactionStats { get; set; } = [];
}