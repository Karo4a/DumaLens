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
    /// Максимальная длина хэша пароля (PBKDF2-SHA256, base64)
    /// </summary>
    public const int PasswordHashMaxLength = 44;

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
}