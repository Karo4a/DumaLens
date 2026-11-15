namespace DumaLens.Api.Models.Bills;

/// <summary>
/// Карточка законопроекта с хронологией голосований
/// </summary>
public class BillApiModel
{
    /// <summary>
    /// Номер законопроекта
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на карточку законопроекта в СОЗД
    /// </summary>
    public string SozdUrl { get; set; } = string.Empty;

    /// <summary>
    /// Голосования по законопроекту в хронологическом порядке
    /// </summary>
    public IReadOnlyCollection<BillVoteApiModel> Votes { get; set; } = [];
}