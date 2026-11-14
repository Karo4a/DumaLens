using DumaLens.Parsing.Contracts.Enums;

namespace DumaLens.Parsing.Contracts.Models;

/// <summary>
/// Замечание разбора голосования (причина карантина)
/// </summary>
public sealed class ParseIssue
{
    /// <summary>
    /// Код замечания
    /// </summary>
    public ParseIssueCode Code { get; set; }

    /// <summary>
    /// Описание замечания для журнала карантина
    /// </summary>
    public string Message { get; set; } = string.Empty;
}