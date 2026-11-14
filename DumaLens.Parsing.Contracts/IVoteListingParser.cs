using DumaLens.Parsing.Contracts.Models;

namespace DumaLens.Parsing.Contracts;

/// <summary>
/// Парсер листинга голосований сайта vote.duma.gov.ru
/// </summary>
public interface IVoteListingParser
{
    /// <summary>
    /// Разбирает HTML страницы листинга (элементы, сгруппированные по датам, и пагинацию)
    /// </summary>
    /// <param name="html">HTML страницы листинга</param>
    /// <returns>Элементы страницы и номера страниц пагинации</returns>
    ParsedListing Parse(string html);
}