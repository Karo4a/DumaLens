namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Сводная статистика фракции по созыву
/// </summary>
public class FactionConvocationStatsModel
{
    /// <summary>
    /// Номер созыва
    /// </summary>
    public int ConvocationNumber { get; set; }

    /// <summary>
    /// Число голосований с участием фракции в созыве
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Средний процент «за» от состава фракции
    /// </summary>
    public decimal? AvgAcceptPctMembers { get; set; }

    /// <summary>
    /// Средний процент «против» от состава фракции
    /// </summary>
    public decimal? AvgDeclinePctMembers { get; set; }

    /// <summary>
    /// Средний процент «воздержался» от состава фракции
    /// </summary>
    public decimal? AvgAbstainPctMembers { get; set; }

    /// <summary>
    /// Средний процент «не голосовал» от состава фракции
    /// </summary>
    public decimal? AvgNonePctMembers { get; set; }
}