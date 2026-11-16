using DumaLens.Entities.Contracts;
using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Entities;

/// <summary>
/// Голосование
/// </summary>
/// <remarks>
/// Заголовок хранится как text без ограничения длины (паспорт данных, 02-data-sources.md, §2.3.9);
/// поисковый вектор TitleSearch — генерируемая tsvector-колонка (теневое свойство конфигурации).
/// </remarks>
public class Vote : DbBaseEntity
{
    /// <summary>
    /// Идентификатор голосования на сайте
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Узкий суррогатный ключ для ссылок из DeputyVotes; генерируется базой (identity)
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// Заголовок голосования
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время голосования (с историческим смещением Москвы)
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

    /// <summary>
    /// Смещение часового пояса источника в минутах (+0300/+0400)
    /// </summary>
    public int UtcOffsetMinutes { get; set; }

    /// <summary>
    /// Идентификатор созыва
    /// </summary>
    public Guid ConvocationId { get; set; }

    /// <summary>
    /// Идентификатор законопроекта (null для процедурных голосований)
    /// </summary>
    public Guid? BillId { get; set; }

    /// <summary>
    /// Каноническая стадия рассмотрения
    /// </summary>
    public VoteStage Stage { get; set; }

    /// <summary>
    /// Исходный префикс стадии из заголовка
    /// </summary>
    public string? StageRaw { get; set; }

    /// <summary>
    /// Итог голосования по данным сайта
    /// </summary>
    public VoteOutcome Outcome { get; set; }

    /// <summary>
    /// Количество голосов «за» (сводно)
    /// </summary>
    public int AcceptCountTotal { get; set; }

    /// <summary>
    /// Количество голосов «против» (сводно)
    /// </summary>
    public int DeclineCountTotal { get; set; }

    /// <summary>
    /// Количество голосов «воздержался» (сводно)
    /// </summary>
    public int AbstainCountTotal { get; set; }

    /// <summary>
    /// Количество голосов «не голосовал» (сводно)
    /// </summary>
    public int NoneCountTotal { get; set; }

    /// <summary>
    /// Процент «за» с сайта (null, если не публиковался)
    /// </summary>
    public decimal? AcceptPercentSite { get; set; }

    /// <summary>
    /// Есть ли результаты (индивидуальные голоса); false для пустых голосований
    /// </summary>
    public bool HasResults { get; set; }

    /// <summary>
    /// Источник данных голосования
    /// </summary>
    public VoteSource Source { get; set; }

    /// <summary>
    /// Созыв
    /// </summary>
    public Convocation Convocation { get; set; } = null!;

    /// <summary>
    /// Законопроект (null для процедурных голосований)
    /// </summary>
    public Bill? Bill { get; set; }
}