namespace DumaLens.Entities.Contracts;

/// <summary>
/// Интерфейс сущности с удалением
/// </summary>
public interface IEntityWithSoftDeleted
{
    /// <summary>
    /// Дата удаления записи
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }
}