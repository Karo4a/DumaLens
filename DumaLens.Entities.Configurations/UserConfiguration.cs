using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="User"/>
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Login)
            .HasMaxLength(UserValidationConstants.LoginMaxLength)
            .IsRequired();
        builder.Property(x => x.LoginLowerCase)
            .HasMaxLength(UserValidationConstants.LoginMaxLength)
            .IsRequired();
        builder.Property(x => x.Email)
            .HasMaxLength(UserValidationConstants.EmailMaxLength)
            .IsRequired();
        builder.Property(x => x.EmailLowerCase)
            .HasMaxLength(UserValidationConstants.EmailMaxLength)
            .IsRequired();
        builder.Property(x => x.Name)
            .HasMaxLength(UserValidationConstants.NameMaxLength)
            .IsRequired();
        builder.Property(x => x.PasswordHash)
            .HasMaxLength(UserValidationConstants.PasswordHashMaxLength)
            .IsRequired();
        builder.Property(x => x.PasswordSalt)
            .HasMaxLength(UserValidationConstants.PasswordSaltMaxLength)
            .IsRequired();
        builder.Property(x => x.Note)
            .HasMaxLength(UserValidationConstants.NoteMaxLength);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.LoginLowerCase, $"IX_{nameof(User)}_{nameof(User.LoginLowerCase)}")
            .IsUnique()
            .HasFilter($"\"{nameof(User.DeletedAt)}\" IS NULL");
        builder.HasIndex(x => x.EmailLowerCase, $"IX_{nameof(User)}_{nameof(User.EmailLowerCase)}")
            .IsUnique()
            // Электронная почта необязательна: пустые значения не участвуют в уникальности
            .HasFilter($"\"{nameof(User.DeletedAt)}\" IS NULL AND \"{nameof(User.Email)}\" <> ''");
        builder.HasIndex(x => x.RoleId, $"IX_{nameof(User)}_{nameof(User.RoleId)}");
    }
}