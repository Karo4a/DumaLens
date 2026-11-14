namespace DumaLens.Entities.Enums;

/// <summary>
/// Итог голосования по данным сайта
/// </summary>
/// <remarks>
/// Итог — только факт с сайта (ADR-9); для голосований архива без итога — <see cref="Unknown"/>.
/// </remarks>
public enum VoteOutcome
{
    /// <summary>
    /// Принят
    /// </summary>
    Accepted = 0,
    /// <summary>
    /// Отклонён
    /// </summary>
    Rejected = 1,
    /// <summary>
    /// Итог неизвестен
    /// </summary>
    Unknown = 2
}