using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Parsing.Contracts.Models;

/// <summary>
/// Индивидуальный голос депутата, извлечённый из карточки сайта или файла архива
/// </summary>
public sealed class ParsedDeputyVote
{
    /// <summary>
    /// Идентификатор депутата на сайте (0, если не извлечён)
    /// </summary>
    public long DeputySiteId { get; set; }

    /// <summary>
    /// Очищенное ФИО депутата
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор фракции на сайте (null, если отсутствует)
    /// </summary>
    public long? FactionSiteId { get; set; }

    /// <summary>
    /// Короткое название фракции (null, если отсутствует)
    /// </summary>
    public string? FactionTitle { get; set; }

    /// <summary>
    /// Канонический результат голоса (null для неизвестного кода — причина карантина)
    /// </summary>
    public VoteResult? Result { get; set; }

    /// <summary>
    /// Исходный код результата источника (accept/decline/… или for/against/…)
    /// </summary>
    public string ResultRaw { get; set; } = string.Empty;
}