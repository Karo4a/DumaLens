namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации пользователя
/// </summary>
public static class UserValidationConstants
{
    /// <summary>
    /// Максимальная длина логина
    /// </summary>
    public const int LoginMaxLength = 255;

    /// <summary>
    /// Максимальная длина электронной почты
    /// </summary>
    public const int EmailMaxLength = 255;

    /// <summary>
    /// Максимальная длина хэша пароля (формат «{итерации}.{base64 PBKDF2-SHA256}»: префикс итераций + 44 символа)
    /// </summary>
    public const int PasswordHashMaxLength = 64;

    /// <summary>
    /// Максимальная длина соли пароля (base64)
    /// </summary>
    public const int PasswordSaltMaxLength = 44;

    /// <summary>
    /// Максимальная длина отображаемого имени
    /// </summary>
    public const int NameMaxLength = 256;

    /// <summary>
    /// Максимальная длина заметки администратора
    /// </summary>
    public const int NoteMaxLength = 1024;

    /// <summary>
    /// Минимальная длина пароля (07-security.md §2.2)
    /// </summary>
    public const int PasswordMinLength = 8;

    /// <summary>
    /// Максимальная длина пароля (ограничение вычислительной нагрузки PBKDF2)
    /// </summary>
    public const int PasswordMaxLength = 128;

    /// <summary>
    /// Минимальный номер страницы списка пользователей
    /// </summary>
    public const int PageMin = 1;

    /// <summary>
    /// Максимальный номер страницы списка пользователей (защита Skip от переполнения int)
    /// </summary>
    public const int PageMax = 100_000;

    /// <summary>
    /// Размер страницы списка пользователей по умолчанию
    /// </summary>
    public const int PageSizeDefault = 20;

    /// <summary>
    /// Минимальный размер страницы списка пользователей
    /// </summary>
    public const int PageSizeMin = 1;

    /// <summary>
    /// Максимальный размер страницы списка пользователей
    /// </summary>
    public const int PageSizeMax = 100;

    /// <summary>
    /// Максимальная длина refresh-токена в запросе (base64, 256 бит)
    /// </summary>
    public const int RefreshTokenMaxLength = 256;
}