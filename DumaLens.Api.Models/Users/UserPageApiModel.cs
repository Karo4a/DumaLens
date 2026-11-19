namespace DumaLens.Api.Models.Users;

/// <summary>
/// Страница списка пользователей
/// </summary>
public class UserPageApiModel
{
    /// <summary>
    /// Общее число пользователей
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Номер страницы
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Пользователи на странице
    /// </summary>
    public IReadOnlyCollection<UserApiModel> Items { get; set; } = [];
}