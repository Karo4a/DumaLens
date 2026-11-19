using System.Security.Cryptography;
using System.Text;

namespace DumaLens.Services.Auth;

/// <summary>
/// Криптография паролей и refresh-токенов (ADR-12, ADR-13)
/// </summary>
/// <remarks>
/// PBKDF2-SHA256: соль 32 байта, хэш 32 байта, не менее 100 000 итераций
/// (в эталонах 6 итераций — осознанное отступление, ADR-12). Сравнение — FixedTimeEquals.
/// Refresh-токен — случайные 256 бит; в базе хранится только SHA-256-хэш токена.
/// </remarks>
public static class SecurityHelper
{
    /// <summary>
    /// Число итераций PBKDF2 по умолчанию (ADR-12: не менее 100 000)
    /// </summary>
    public const int DefaultIterations = 100_000;

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

    /// <summary>
    /// Генерирует криптографически случайную соль (base64)
    /// </summary>
    public static string GenerateSalt()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeBytes));

    /// <summary>
    /// Вычисляет солированный хэш пароля PBKDF2-SHA256 (base64)
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <param name="salt">Соль (base64)</param>
    /// <param name="iterations">Число итераций (по умолчанию <see cref="DefaultIterations"/>)</param>
    public static string HashPassword(string password, string salt, int iterations = DefaultIterations)
        => Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            password,
            Convert.FromBase64String(salt),
            iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes));

    /// <summary>
    /// Проверяет пароль по хэшу и соли за постоянное время (защита от timing-атак)
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <param name="salt">Соль (base64)</param>
    /// <param name="expectedHash">Ожидаемый хэш (base64)</param>
    /// <param name="iterations">Число итераций (по умолчанию <see cref="DefaultIterations"/>)</param>
    public static bool VerifyPassword(string password, string salt, string expectedHash, int iterations = DefaultIterations)
    {
        try
        {
            var actual = Convert.FromBase64String(HashPassword(password, salt, iterations));
            var expected = Convert.FromBase64String(expectedHash);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

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
}