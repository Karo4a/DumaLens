using DumaLens.Entities.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель роли пользователя для запроса из базы данных
/// </summary>
public class UserRoleDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Роль
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Описание роли
    /// </summary>
    public string Description { get; set; } = string.Empty;
}