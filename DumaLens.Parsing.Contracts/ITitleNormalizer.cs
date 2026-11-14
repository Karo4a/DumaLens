using DumaLens.Parsing.Contracts.Models;

namespace DumaLens.Parsing.Contracts;

/// <summary>
/// Нормализатор заголовка голосования (очистка, номер законопроекта, стадия)
/// </summary>
public interface ITitleNormalizer
{
    /// <summary>
    /// Нормализует заголовок: очистка, извлечение номера законопроекта, определение стадии по словарю
    /// </summary>
    /// <param name="title">Исходный заголовок</param>
    /// <returns>Очищенный заголовок, номер законопроекта и каноническая стадия</returns>
    NormalizedTitle Normalize(string title);
}