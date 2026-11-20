using DumaLens.Services.Contracts;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DumaLens.Services.Auth;

/// <summary>
/// Криптография паролей и refresh-токенов (ADR-12, ADR-13)
/// </summary>
/// <remarks>
/// PBKDF2-SHA256: соль 32 байта, хэш 32 байта, не менее 100 000 итераций
/// (в эталонах 6 итераций — осознанное отступление, ADR-12). Сравнение — FixedTimeEquals.
/// Число итераций хранится в самом хэше (префикс «{итерации}.»): смена Auth:PasswordIterations
/// не ломает существующие пароли — проверка использует значение из хэша, а не из конфигурации.
/// Хэши без префикса (legacy) проверяются с переданным числом итераций.
/// Refresh-токен — случайные 256 бит; в базе хранится только SHA-256-хэш токена.
/// </remarks>
public static class SecurityHelper
{
    /// <summary>
    /// Число итераций PBKDF2 по умолчанию (ADR-12: не менее 100 000)
    /// </summary>
    public const int DefaultIterations = AuthOptions.MinimumPasswordIterations;

    /// <summary>
    /// Размер соли пароля, байт
    /// </summary>
    public const int SaltSizeBytes = 32;

    /// <summary>
    /// Размер хэша пароля, байт
    /// </summary>
    public const int HashSizeBytes = 32;

    /// <summary>
    /// Размер refresh-токена, байт (256 бит)
    /// </summary>
    public const int RefreshTokenSizeBytes = 32;

    private const string TemporaryPasswordAlphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const char IterationsSeparator = '.';

    /// <summary>
    /// Генерирует криптографически случайную соль (base64)
    /// </summary>
    public static string GenerateSalt()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeBytes));

    /// <summary>
    /// Вычисляет солированный хэш пароля PBKDF2-SHA256 в формате «{итерации}.{base64-хэш}»
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <param name="salt">Соль (base64)</param>
    /// <param name="iterations">Число итераций (по умолчанию <see cref="DefaultIterations"/>)</param>
    public static string HashPassword(string password, string salt, int iterations = DefaultIterations)
        => iterations.ToString(CultureInfo.InvariantCulture) + IterationsSeparator + ComputeHash(password, salt, iterations);

    /// <summary>
    /// Проверяет пароль по хэшу и соли за постоянное время (защита от timing-атак).
    /// Число итераций берётся из префикса хэша; для legacy-хэшей без префикса — <paramref name="iterations"/>
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <param name="salt">Соль (base64)</param>
    /// <param name="expectedHash">Ожидаемый хэш («{итерации}.{base64}» либо legacy-base64)</param>
    /// <param name="iterations">Число итераций для legacy-хэшей без префикса</param>
    public static bool VerifyPassword(string password, string salt, string expectedHash, int iterations = DefaultIterations)
    {
        try
        {
            var (hashIterations, hash) = ParseHash(expectedHash, iterations);
            var actual = Convert.FromBase64String(ComputeHash(password, salt, hashIterations));
            var expected = Convert.FromBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Извлекает число итераций из хэша с префиксом; legacy-хэш без префикса — <paramref name="fallbackIterations"/>
    /// </summary>
    /// <param name="expectedHash">Хэш пароля</param>
    /// <param name="fallbackIterations">Число итераций для legacy-хэшей без префикса</param>
    public static int GetIterations(string expectedHash, int fallbackIterations = DefaultIterations)
        => ParseHash(expectedHash, fallbackIterations).Iterations;

    /// <summary>
    /// Генерирует refresh-токен — случайные 256 бит (base64)
    /// </summary>
    public static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes));

    /// <summary>
    /// Вычисляет хэш refresh-токена для хранения в базе (SHA-256, base64)
    /// </summary>
    /// <param name="refreshToken">Исходный refresh-токен</param>
    public static string HashRefreshToken(string refreshToken)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    /// <summary>
    /// Генерирует временный пароль из криптослучайных символов (без визуально похожих знаков)
    /// </summary>
    /// <param name="length">Длина пароля (не менее 8)</param>
    public static string GenerateTemporaryPassword(int length = 16)
    {
        if (length < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Длина временного пароля должна быть не менее 8");
        }
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = TemporaryPasswordAlphabet[RandomNumberGenerator.GetInt32(TemporaryPasswordAlphabet.Length)];
        }
        return new string(chars);
    }

    private static string ComputeHash(string password, string salt, int iterations)
        => Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            password,
            Convert.FromBase64String(salt),
            iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes));

    private static (int Iterations, string Hash) ParseHash(string expectedHash, int fallbackIterations)
    {
        var separatorIndex = expectedHash.IndexOf(IterationsSeparator);
        if (separatorIndex < 0)
        {
            return (fallbackIterations, expectedHash);
        }
        var iterations = int.Parse(expectedHash[..separatorIndex], CultureInfo.InvariantCulture);
        if (iterations < 1)
        {
            throw new FormatException("Число итераций в хэше пароля должно быть положительным");
        }
        return (iterations, expectedHash[(separatorIndex + 1)..]);
    }
}