namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Страница списка пользователей
/// </summary>
public class UserPageModel
{
    /// <summary>
    /// Общее число пользователей (до пагинации)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Номер текущей страницы (с 1)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Строки текущей страницы
    /// </summary>
    public IReadOnlyCollection<UserListItemModel> Items { get; set; } = [];
}