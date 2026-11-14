namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации refresh-токена
/// </summary>
public static class RefreshTokenValidationConstants
{
    /// <summary>
    /// Максимальная длина хэша refresh-токена (base64)
    /// </summary>
    public const int TokenHashMaxLength = 44;
}