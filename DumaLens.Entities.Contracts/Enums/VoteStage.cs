namespace DumaLens.Entities.Contracts.Enums;

/// <summary>
/// Каноническая стадия рассмотрения законопроекта
/// </summary>
/// <remarks>
/// Словарь вариантов написания — 04-functionality.md, §2.2; исходный префикс заголовка сохраняется в StageRaw.
/// </remarks>
public enum VoteStage
{
    /// <summary>
    /// Первое чтение
    /// </summary>
    FirstReading = 0,
    /// <summary>
    /// Второе чтение (без уточнения таблицы поправок)
    /// </summary>
    SecondReading = 1,
    /// <summary>
    /// Второе чтение, таблица 1 — принятые поправки
    /// </summary>
    SecondReadingAcceptedAmendments = 2,
    /// <summary>
    /// Второе чтение, таблица 2 — отклонённые поправки
    /// </summary>
    SecondReadingRejectedAmendments = 3,
    /// <summary>
    /// Третье чтение
    /// </summary>
    ThirdReading = 4,
    /// <summary>
    /// Принятие за основу
    /// </summary>
    AsBasis = 5,
    /// <summary>
    /// Принятие в целом
    /// </summary>
    AsWhole = 6,
    /// <summary>
    /// К отклонению
    /// </summary>
    Reject = 7,
    /// <summary>
    /// Возврат во второе чтение
    /// </summary>
    ReturnToSecondReading = 8,
    /// <summary>
    /// Процедурное голосование (порядок работы, регламент, предложения депутатов)
    /// </summary>
    Procedural = 9,
    /// <summary>
    /// Стадия не распознана
    /// </summary>
    Unknown = 10
}