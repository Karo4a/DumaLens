using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения голосований: поиск по спецификации
/// </summary>
public interface IVoteReadRepository
{
    /// <summary>
    /// Ищет голосования по спецификации: полнотекстовый фильтр, фильтры, фракционные условия по И, сортировка, пагинация
    /// </summary>
    /// <param name="spec">Спецификация поиска (ссылки разрешены сервисом до идентификаторов)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Страница результатов с общим числом совпадений и фракционными строками</returns>
    /// <remarks>
    /// Полнотекстовый фильтр (<see cref="VoteSearchSpec.Query"/>) и сортировка по релевантности
    /// поддерживаются только PostgreSQL (ADR-1); на InMemory не используются.
    /// Фракционные условия выполняются по строкам <c>VoteFactionStats</c> (EXISTS); строки с NULL-процентом
    /// (пустой состав, никто не голосовал) условию не удовлетворяют.
    /// </remarks>
    Task<VoteSearchPageDbModel> Search(VoteSearchSpec spec, CancellationToken cancellationToken);
}