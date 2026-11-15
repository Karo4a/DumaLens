namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Статистика фракции по голосованию
/// </summary>
public class VoteFactionStatsApiModel
{
    /// <summary>
    /// Код канонической фракции
    /// </summary>
    public string FactionCode { get; set; } = string.Empty;

    /// <summary>
    /// Название канонической фракции
    /// </summary>
    public string FactionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Состав фракции на момент голосования
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Число проголосовавших депутатов фракции
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
    /// Число голосов «не голосовал»
    /// </summary>
    public int NoneCount { get; set; }

    /// <summary>
    /// Процент «за» от состава фракции
    /// </summary>
    public decimal? AcceptPctMembers { get; set; }

    /// <summary>
    /// Процент «против» от состава фракции
    /// </summary>
    public decimal? DeclinePctMembers { get; set; }

    /// <summary>
    /// Процент «воздержался» от состава фракции
    /// </summary>
    public decimal? AbstainPctMembers { get; set; }

    /// <summary>
    /// Процент «не голосовал» от состава фракции
    /// </summary>
    public decimal? NonePctMembers { get; set; }

    /// <summary>
    /// Процент «за» от проголосовавших
    /// </summary>
    public decimal? AcceptPctVoted { get; set; }

    /// <summary>
    /// Процент «против» от проголосовавших
    /// </summary>
    public decimal? DeclinePctVoted { get; set; }

    /// <summary>
    /// Процент «воздержался» от проголосовавших
    /// </summary>
    public decimal? AbstainPctVoted { get; set; }
}