using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис поиска голосований
/// </summary>
public interface IVoteSearchService
{
    /// <summary>
    /// Ищет голосования по запросу: валидация параметров, резолвинг ссылок, поиск, маппинг в модель сервиса
    /// </summary>
    /// <param name="query">Запрос поиска</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Страница выдачи с общим числом совпадений</returns>
    /// <exception cref="Exceptions.DumaLensValidationException">
    /// Некорректные параметры запроса (пороги, стадии, пагинация, даты, число условий) или неизвестная фракция
    /// </exception>
    /// <remarks>
    /// Неизвестный номер законопроекта или созыва даёт пустую выдачу (фильтр без совпадений), а не ошибку.
    /// </remarks>
    Task<VoteSearchResultModel> Search(VoteSearchQueryModel query, CancellationToken cancellationToken);
}