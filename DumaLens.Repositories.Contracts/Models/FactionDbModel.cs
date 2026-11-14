namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель фракции для запроса из базы данных
/// </summary>
public class FactionDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Код канонической фракции
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Название фракции
    /// </summary>
    public string Title { get; set; } = string.Empty;
}