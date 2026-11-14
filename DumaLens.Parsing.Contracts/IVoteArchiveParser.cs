using DumaLens.Parsing.Contracts.Models;

namespace DumaLens.Parsing.Contracts;

/// <summary>
/// Парсер JSON-файла голосования архива vote-master.zip
/// </summary>
public interface IVoteArchiveParser
{
    /// <summary>
    /// Разбирает JSON файла архива (id, title, datetime со смещением, votes)
    /// </summary>
    /// <param name="json">Содержимое файла архива</param>
    /// <returns>Результат разбора с замечаниями карантина</returns>
    ParsedVote Parse(string json);
}