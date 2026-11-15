namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Созыв в каталоге с числом голосований
/// </summary>
public class ConvocationCatalogDbModel : ConvocationDbModel
{
    /// <summary>
    /// Число голосований в созыве
    /// </summary>
    public int VoteCount { get; set; }
}