namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации роли пользователя
/// </summary>
public static class UserRoleValidationConstants
{
    /// <summary>
    /// Максимальная длина имени роли (строковое значение enum)
    /// </summary>
    public const int NameMaxLength = 32;

    /// <summary>
    /// Максимальная длина описания роли
    /// </summary>
    public const int DescriptionMaxLength = 512;
}