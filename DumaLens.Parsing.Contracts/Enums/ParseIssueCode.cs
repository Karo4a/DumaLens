namespace DumaLens.Parsing.Contracts.Enums;

/// <summary>
/// Код причины карантина разбора голосования
/// </summary>
/// <remarks>
/// Нарушения фиксируются как <see cref="Models.ParseIssue"/> и при импорте попадают в ImportQuarantine (Э3).
/// </remarks>
public enum ParseIssueCode
{
    /// <summary>
    /// Некорректный JSON (файл архива или массив deputiesData)
    /// </summary>
    InvalidJson = 0,
    /// <summary>
    /// На странице карточки не найден массив deputiesData
    /// </summary>
    MissingDeputiesData = 1,
    /// <summary>
    /// Пустой заголовок голосования
    /// </summary>
    EmptyTitle = 2,
    /// <summary>
    /// Идентификатор голосования отсутствует или некорректен
    /// </summary>
    InvalidVoteId = 3,
    /// <summary>
    /// Дата голосования отсутствует или не распознана
    /// </summary>
    InvalidDate = 4,
    /// <summary>
    /// Неизвестный код результата голоса депутата
    /// </summary>
    UnknownResultCode = 5,
    /// <summary>
    /// Не удалось извлечь идентификатор депутата
    /// </summary>
    MissingDeputyId = 6,
    /// <summary>
    /// Число депутатов вне допустимого диапазона (0 или 430–460)
    /// </summary>
    DeputyCountOutOfRange = 7,
    /// <summary>
    /// Дата голосования вне фактических границ созыва
    /// </summary>
    DateOutOfConvocationRange = 8
}