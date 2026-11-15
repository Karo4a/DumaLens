namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Позиция фракции в голосовании по законопроекту (проценты от состава)
/// </summary>
public class BillFactionPositionModel
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
}