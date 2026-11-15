namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Фракционная строка голосования в выдаче поиска
/// </summary>
public class VoteFactionStatsItemModel
{
    /// <summary>
    /// Идентификатор канонической фракции
    /// </summary>
    public Guid FactionId { get; set; }

    /// <summary>
    /// Код канонической фракции (например, ER)
    /// </summary>
    public string FactionCode { get; set; } = string.Empty;

    /// <summary>
    /// Название канонической фракции
    /// </summary>
    public string FactionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Списочный состав фракции на момент голосования
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Число голосовавших (за + против + воздержался)
    /// </summary>
    public int VotedCount { get; set; }

    /// <summary>
    /// Число голосов «за»
    /// </summary>
    public int AcceptCount { get; set; }

    /// <summary>
    /// Число голосов «против»
    /// </summary>
    public int DeclineCount { get; set; }

    /// <summary>
    /// Число голосов «воздержался»
    /// </summary>
    public int AbstainCount { get; set; }

    /// <summary>
    /// Число не голосовавших членов фракции
    /// </summary>
    public int NoneCount { get; set; }

    /// <summary>
    /// Процент «за» от списочного состава (null при пустом составе)
    /// </summary>
    public decimal? AcceptPctMembers { get; set; }

    /// <summary>
    /// Процент «против» от списочного состава (null при пустом составе)
    /// </summary>
    public decimal? DeclinePctMembers { get; set; }

    /// <summary>
    /// Процент «воздержался» от списочного состава (null при пустом составе)
    /// </summary>
    public decimal? AbstainPctMembers { get; set; }

    /// <summary>
    /// Процент «не голосовал» от списочного состава (null при пустом составе)
    /// </summary>
    public decimal? NonePctMembers { get; set; }

    /// <summary>
    /// Процент «за» от проголосовавших (null, если никто не голосовал)
    /// </summary>
    public decimal? AcceptPctVoted { get; set; }

    /// <summary>
    /// Процент «против» от проголосовавших (null, если никто не голосовал)
    /// </summary>
    public decimal? DeclinePctVoted { get; set; }

    /// <summary>
    /// Процент «воздержался» от проголосовавших (null, если никто не голосовал)
    /// </summary>
    public decimal? AbstainPctVoted { get; set; }
}