using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Parsing.Contracts.Models;

/// <summary>
/// Результат нормализации заголовка голосования
/// </summary>
public sealed class NormalizedTitle
{
    /// <summary>
    /// Очищенный заголовок (декодированные сущности, схлопнутые пробелы)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Номер законопроекта (null для процедурных голосований и постановлений без номера)
    /// </summary>
    public string? BillNumber { get; set; }

    /// <summary>
    /// Каноническая стадия рассмотрения
    /// </summary>
    public VoteStage Stage { get; set; } = VoteStage.Unknown;

    /// <summary>
    /// Исходный префикс стадии из заголовка (сохраняется для нераспознанных стадий)
    /// </summary>
    public string? StageRaw { get; set; }
}