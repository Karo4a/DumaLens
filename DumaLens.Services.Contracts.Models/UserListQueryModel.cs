namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос страницы списка пользователей
/// </summary>
public class UserListQueryModel
{
    /// <summary>
    /// Номер страницы (с 1)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; } = 20;
}