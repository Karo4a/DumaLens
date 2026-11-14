using DumaLens.Entities;
using DumaLens.Entities.Enums;

namespace DumaLens.Entities.Default;

/// <summary>
/// Стандартные роли пользователей
/// </summary>
/// <remarks>
/// Три роли с фиксированными идентификаторами (07-security.md, §1); иерархия User ⊂ Moderator ⊂ Administrator.
/// </remarks>
public class UserRolesDefault
{
    /// <summary>
    /// Сущности стандартных ролей
    /// </summary>
    public static List<UserRole> Entities = new List<UserRole>()
    {
        new UserRole()
        {
            Id = DefaultRoleToGuid(Role.User),
            Role = Role.User,
            Description = "Зарегистрированный читатель: публичные данные, профиль, оценки важности (v1.2)",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new UserRole()
        {
            Id = DefaultRoleToGuid(Role.Moderator),
            Role = Role.Moderator,
            Description = "Ведёт справочники и теги: права пользователя + модерация тегов (v1.1), правка нормализации",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new UserRole()
        {
            Id = DefaultRoleToGuid(Role.Administrator),
            Role = Role.Administrator,
            Description = "Управляет системой: права модератора + пользователи и роли, блокировка, сброс паролей, служебные операции",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
    };

    /// <summary>
    /// Возвращает фиксированный идентификатор роли
    /// </summary>
    /// <param name="role">Роль</param>
    /// <returns>Идентификатор роли или <see cref="Guid.Empty"/>, если роль неизвестна</returns>
    public static Guid DefaultRoleToGuid(Role role)
    {
        switch (role)
        {
            case Role.User: return Guid.Parse("781c1977-48c9-43ff-9091-6ed35bf720ed");
            case Role.Moderator: return Guid.Parse("658ab516-79a2-49ae-afec-98a9b2ca35e2");
            case Role.Administrator: return Guid.Parse("f8a38ae3-c73e-4751-91ad-856e29c0df4c");
            default: return Guid.Empty;
        }
    }
}