using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения законопроектов
/// </summary>
public interface IBillReadRepository
{
    /// <summary>
    /// Возвращает законопроект по точному номеру
    /// </summary>
    /// <param name="number">Номер законопроекта (например, 500091-8)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Модель законопроекта или null, если номер не найден</returns>
    Task<BillDbModel?> GetByNumber(string number, CancellationToken cancellationToken);

    /// <summary>
    /// Получает карточку законопроекта с хронологией голосований (null — не найден)
    /// </summary>
    Task<BillDetailsDbModel?> GetDetailsByNumber(string number, CancellationToken cancellationToken);

    /// <summary>
    /// Ищет законопроекты по части номера (автодополнение, не более limit записей)
    /// </summary>
    Task<IReadOnlyCollection<BillDbModel>> SearchByNumberPart(string numberPart, int limit, CancellationToken cancellationToken);
}