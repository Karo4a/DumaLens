namespace DumaLens.Services.Contracts;

/// <summary>
/// Настройки аутентификации (секция конфигурации «Auth», 07-security.md)
/// </summary>
/// <remarks>
/// Ключ подписи JWT и строка подключения — только конфигурация/переменные окружения/user-secrets (О-7);
/// в репозиторий не попадают. В Development при пустом ключе Api генерирует временный ключ на процесс.
/// Корректность значений проверяется методом <see cref="Validate"/> при старте Api (fail-fast).
/// </remarks>
public class AuthOptions
{
    /// <summary>
    /// Имя секции конфигурации
    /// </summary>
    public const string SectionName = "Auth";

    /// <summary>
    /// Минимальный размер ключа подписи JWT, байт (256 бит — уровень стойкости HMAC-SHA256)
    /// </summary>
    public const int MinimumSigningKeyBytes = 32;

    /// <summary>
    /// Минимальное число итераций PBKDF2 (ADR-12)
    /// </summary>
    public const int MinimumPasswordIterations = 100_000;

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
    /// Число итераций PBKDF2-SHA256 для новых хэшей (ADR-12: не менее 100 000).
    /// Хранится в префиксе каждого хэша: смена значения не ломает существующие пароли
    /// </summary>
    public int PasswordIterations { get; set; } = MinimumPasswordIterations;

    /// <summary>
    /// Число неудачных попыток входа до временной блокировки (по умолчанию 5)
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Длительность блокировки после серии неудачных попыток, минуты (по умолчанию 15)
    /// </summary>
    public int LoginLockoutMinutes { get; set; } = 15;

    /// <summary>
    /// Rate limit эндпоинтов логина и смены пароля: число запросов в окне на один IP (по умолчанию 20)
    /// </summary>
    public int LoginRateLimitPermitLimit { get; set; } = 20;

    /// <summary>
    /// Rate limit эндпоинтов логина и смены пароля: длительность фиксированного окна, секунды (по умолчанию 60)
    /// </summary>
    public int LoginRateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Окно повторного использования отозванного refresh-токена, секунды (по умолчанию 30).
    /// Внутри окна предъявление только что отозванного токена с актуальной меткой безопасности
    /// считается гонкой параллельных обновлений (вкладки/устройства) и выпускает новую пару,
    /// а не отзывом всех сессий; вне окна — признак кражи
    /// </summary>
    public int RefreshReuseGraceSeconds { get; set; } = 30;

    /// <summary>
    /// Срок хранения истёкших и отозванных refresh-токенов после утраты силы, дни (по умолчанию 7).
    /// Фоновая очистка Api удаляет записи старше срока; активные токены не затрагиваются
    /// </summary>
    public int RefreshTokenRetentionDays { get; set; } = 7;

    /// <summary>
    /// Интервал фоновой очистки refresh-токенов, часы (по умолчанию 24)
    /// </summary>
    public int TokenCleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Проверяет корректность настроек; некорректные значения — отказ запуска с понятной ошибкой
    /// </summary>
    public void Validate()
    {
        if (!string.IsNullOrWhiteSpace(SigningKey))
        {
            byte[] keyBytes;
            try
            {
                keyBytes = Convert.FromBase64String(SigningKey);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Auth:SigningKey должен быть строкой base64 (О-7: переменная окружения Auth__SigningKey или user-secrets)");
            }
            if (keyBytes.Length < MinimumSigningKeyBytes)
            {
                throw new InvalidOperationException($"Auth:SigningKey должен содержать не менее {MinimumSigningKeyBytes} байт (256 бит) после декодирования base64; получено {keyBytes.Length}");
            }
        }
        if (AccessTokenLifetimeMinutes < 1)
        {
            throw new InvalidOperationException("Auth:AccessTokenLifetimeMinutes должен быть не менее 1");
        }
        if (RefreshTokenLifetimeDays < 1)
        {
            throw new InvalidOperationException("Auth:RefreshTokenLifetimeDays должен быть не менее 1");
        }
        if (PasswordIterations < MinimumPasswordIterations)
        {
            throw new InvalidOperationException($"Auth:PasswordIterations должен быть не менее {MinimumPasswordIterations} (ADR-12)");
        }
        if (MaxFailedLoginAttempts < 1)
        {
            throw new InvalidOperationException("Auth:MaxFailedLoginAttempts должен быть не менее 1");
        }
        if (LoginLockoutMinutes < 1)
        {
            throw new InvalidOperationException("Auth:LoginLockoutMinutes должен быть не менее 1");
        }
        if (LoginRateLimitPermitLimit < 1)
        {
            throw new InvalidOperationException("Auth:LoginRateLimitPermitLimit должен быть не менее 1");
        }
        if (LoginRateLimitWindowSeconds < 1)
        {
            throw new InvalidOperationException("Auth:LoginRateLimitWindowSeconds должен быть не менее 1");
        }
        if (RefreshReuseGraceSeconds < 0)
        {
            throw new InvalidOperationException("Auth:RefreshReuseGraceSeconds не может быть отрицательным");
        }
        if (RefreshTokenRetentionDays < 1)
        {
            throw new InvalidOperationException("Auth:RefreshTokenRetentionDays должен быть не менее 1");
        }
        if (TokenCleanupIntervalHours < 1)
        {
            throw new InvalidOperationException("Auth:TokenCleanupIntervalHours должен быть не менее 1");
        }
    }
}