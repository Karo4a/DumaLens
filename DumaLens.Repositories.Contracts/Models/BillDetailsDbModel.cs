namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Карточка законопроекта с хронологией голосований
/// </summary>
public class BillDetailsDbModel
{
    /// <summary>
    /// Идентификатор законопроекта
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Номер законопроекта
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Голосования по законопроекту в хронологическом порядке
    /// </summary>
    public IReadOnlyCollection<BillVoteDbModel> Votes { get; set; } = [];
}