using DumaLens.Parsing.Contracts.Models;

namespace DumaLens.Parsing.Contracts;

/// <summary>
/// Валидатор результата разбора голосования (причины карантина)
/// </summary>
public interface IVoteParseValidator
{
    /// <summary>
    /// Проверяет результат разбора: замечания парсера, непустой заголовок, корректность даты,
    /// известные коды результатов, число депутатов (0 или 430–460), границы созыва
    /// </summary>
    /// <param name="vote">Результат разбора</param>
    /// <param name="convocationDateFrom">Дата начала созыва (null — проверка границ не выполняется)</param>
    /// <param name="convocationDateTo">Дата окончания созыва (null — проверка границ не выполняется)</param>
    /// <returns>Список замечаний; пустой список — голосование пригодно к импорту</returns>
    IReadOnlyList<ParseIssue> Validate(
        ParsedVote vote,
        DateOnly? convocationDateFrom = null,
        DateOnly? convocationDateTo = null);
}