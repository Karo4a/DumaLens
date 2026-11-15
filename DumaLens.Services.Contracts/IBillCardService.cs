using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис карточек и поиска законопроектов
/// </summary>
public interface IBillCardService
{
    /// <summary>
    /// Получает карточку законопроекта с хронологией голосований
    /// </summary>
    /// <exception cref="Exceptions.DumaLensNotFoundException">Законопроект не найден</exception>
    Task<BillCardModel> GetCard(string number, CancellationToken cancellationToken);

    /// <summary>
    /// Ищет законопроекты по части номера (автодополнение)
    /// </summary>
    Task<IReadOnlyCollection<BillSearchItemModel>> Search(BillSearchQueryModel query, CancellationToken cancellationToken);
}