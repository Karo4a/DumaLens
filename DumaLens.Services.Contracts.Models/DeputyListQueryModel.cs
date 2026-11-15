namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос списка депутатов созыва
/// </summary>
public class DeputyListQueryModel
{
    /// <summary>
    /// Номер созыва
    /// </summary>
    public int Convocation { get; set; }
}