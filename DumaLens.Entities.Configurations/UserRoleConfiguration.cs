using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="UserRole"/>
/// </summary>
/// <remarks>
/// Таблица Roles; роль хранится строкой в колонке Name; три роли с фиксированными идентификаторами — seed.
/// </remarks>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .HasColumnName("Name")
            .HasConversion<string>()
            .HasMaxLength(UserRoleValidationConstants.NameMaxLength);
        builder.Property(x => x.Description)
            .HasMaxLength(UserRoleValidationConstants.DescriptionMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.Role, $"IX_Roles_{nameof(UserRole.Role)}")
            .IsUnique()
            .HasFilter($"\"{nameof(UserRole.DeletedAt)}\" IS NULL");

        builder.HasData(UserRolesDefault.Entities);
    }
}