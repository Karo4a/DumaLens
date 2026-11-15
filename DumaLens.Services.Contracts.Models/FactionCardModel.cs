namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Карточка канонической фракции
/// </summary>
public class FactionCardModel
{
    /// <summary>
    /// Код канонической фракции
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Название канонической фракции
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Исторические периоды фракции по созывам
    /// </summary>
    public IReadOnlyCollection<FactionPeriodModel> Periods { get; set; } = [];

    /// <summary>
    /// Сводная статистика по созывам (заполняется только в карточке фракции)
    /// </summary>
    public IReadOnlyCollection<FactionConvocationStatsModel> ConvocationStats { get; set; } = [];
}