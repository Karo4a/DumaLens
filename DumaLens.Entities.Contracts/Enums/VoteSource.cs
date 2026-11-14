namespace DumaLens.Entities.Contracts.Enums;

/// <summary>
/// Источник данных голосования
/// </summary>
public enum VoteSource
{
    /// <summary>
    /// Архив vote-master.zip
    /// </summary>
    Archive = 0,
    /// <summary>
    /// Живой сайт vote.duma.gov.ru
    /// </summary>
    Site = 1
}