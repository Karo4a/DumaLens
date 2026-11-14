using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Parsing.Contracts.Models;

/// <summary>
/// Результат разбора голосования (карточка сайта или файл архива)
/// </summary>
/// <remarks>
/// Нормализация заголовка (номер законопроекта, стадия) выполняется отдельно — <see cref="ITitleNormalizer"/>.
/// </remarks>
public sealed class ParsedVote
{
    /// <summary>
    /// Идентификатор голосования на сайте
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Очищенный заголовок (декодированные сущности, схлопнутые пробелы)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Исходный заголовок без потерь
    /// </summary>
    public string TitleRaw { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время голосования с историческим смещением Москвы
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

    /// <summary>
    /// Смещение часового пояса источника в минутах (+0300/+0400)
    /// </summary>
    public int UtcOffsetMinutes => (int)VotedAt.Offset.TotalMinutes;

    /// <summary>
    /// Код созыва на сайте (например, AAAAAAA8); null для архива — созыв определяется по дате
    /// </summary>
    public string? ConvocationSiteCode { get; set; }

    /// <summary>
    /// Источник данных
    /// </summary>
    public VoteSource Source { get; set; }

    /// <summary>
    /// Есть ли результаты (индивидуальные голоса); false для пустых голосований
    /// </summary>
    public bool HasResults => DeputyVotes.Count > 0;

    /// <summary>
    /// Индивидуальные голоса депутатов
    /// </summary>
    public IReadOnlyList<ParsedDeputyVote> DeputyVotes { get; set; } = [];

    /// <summary>
    /// Замечания, выявленные при разборе (структурные нарушения)
    /// </summary>
    public IReadOnlyList<ParseIssue> Issues { get; set; } = [];
}