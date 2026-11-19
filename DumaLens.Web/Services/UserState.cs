namespace DumaLens.Web.Services;

/// <summary>
/// Состояние текущего пользователя цепи Blazor ( scoped на цепь)
/// </summary>
/// <remarks>
/// Заполняется из Api (<c>UserStateInitializer</c> после подключения цепи или после входа),
/// а не разбором токена на клиенте. <see cref="IsInitialized"/> отличает «ещё не восстановили»
/// от «восстановили, пользователь анонимный» — защищённые страницы показывают загрузку до инициализации.
/// </remarks>
public class UserState
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid? Id { get; private set; }

    /// <summary>
    /// Логин
    /// </summary>
    public string? Login { get; private set; }

    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Электронная почта
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Роль (User, Moderator, Administrator)
    /// </summary>
    public string? Role { get; private set; }

    /// <summary>
    /// Пароль временный: требуется смена
    /// </summary>
    public bool PasswordIsTemporary { get; private set; }

    /// <summary>
    /// Начальное восстановление состояния завершено (в том числе для анонимного пользователя)
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Пользователь аутентифицирован
    /// </summary>
    public bool IsAuthenticated => Id.HasValue;

    /// <summary>
    /// Роль Administrator
    /// </summary>
    public bool IsAdministrator => Role == "Administrator";

    /// <summary>
    /// Роль Moderator или Administrator (иерархия ролей)
    /// </summary>
    public bool IsModerator => Role is "Moderator" or "Administrator";

    /// <summary>
    /// Событие изменения состояния
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Заполняет состояние из профиля Api
    /// </summary>
    public void SetFrom(Guid id, string login, string name, string email, string role, bool passwordIsTemporary)
    {
        Id = id;
        Login = login;
        Name = name;
        Email = email;
        Role = role;
        PasswordIsTemporary = passwordIsTemporary;
        IsInitialized = true;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Сбрасывает состояние (выход, недействительные токены); фиксирует завершение инициализации
    /// </summary>
    public void Clear()
    {
        Id = null;
        Login = null;
        Name = null;
        Email = null;
        Role = null;
        PasswordIsTemporary = false;
        IsInitialized = true;
        OnChange?.Invoke();
    }
}