namespace DumaLens.Services.Contracts;

/// <summary>
/// Настройки аутентификации (секция конфигурации «Auth», 07-security.md)
/// </summary>
/// <remarks>
/// Ключ подписи JWT и строка подключения — только конфигурация/переменные окружения/user-secrets (О-7);
/// в репозиторий не попадают. В Development при пустом ключе Api генерирует временный ключ на процесс.
/// </remarks>
public class AuthOptions
{
    /// <summary>
    /// Имя секции конфигурации
    /// </summary>
    public const string SectionName = "Auth";

    /// <summary>
    /// Издатель токена (issuer)
    /// </summary>
    public string Issuer { get; set; } = "DumaLens.Api";

    /// <summary>
    /// Аудитория токена (audience)
    /// </summary>
    public string Audience { get; set; } = "DumaLens.Web";

    /// <summary>
    /// Ключ подписи JWT (base64, не менее 32 байт); пустой — временный ключ в Development
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Срок жизни access-токена, минуты (по умолчанию 15)
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// Срок жизни refresh-токена, дни (по умолчанию 30)
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>
    /// Число итераций PBKDF2-SHA256 (ADR-12: не менее 100 000)
    /// </summary>
    public int PasswordIterations { get; set; } = 100_000;

    /// <summary>
    /// Число неудачных попыток входа до временной блокировки (по умолчанию 5)
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Длительность блокировки после серии неудачных попыток, минуты (по умолчанию 15)
    /// </summary>
    public int LoginLockoutMinutes { get; set; } = 15;

    /// <summary>
    /// Rate limit эндпоинта логина: число запросов в окне на один IP (по умолчанию 20)
    /// </summary>
    public int LoginRateLimitPermitLimit { get; set; } = 20;

    /// <summary>
    /// Rate limit эндпоинта логина: длительность фиксированного окна, секунды (по умолчанию 60)
    /// </summary>
    public int LoginRateLimitWindowSeconds { get; set; } = 60;
}