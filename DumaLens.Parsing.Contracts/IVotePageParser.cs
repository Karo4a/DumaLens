using DumaLens.Parsing.Contracts.Models;

namespace DumaLens.Parsing.Contracts;

/// <summary>
/// Парсер карточки голосования сайта vote.duma.gov.ru (/vote/{id})
/// </summary>
public interface IVotePageParser
{
    /// <summary>
    /// Разбирает HTML карточки голосования
    /// </summary>
    /// <param name="html">HTML страницы /vote/{id}</param>
    /// <param name="siteId">Идентификатор голосования на сайте (из URL)</param>
    /// <returns>Результат разбора с замечаниями карантина</returns>
    ParsedVote Parse(string html, long siteId);
}