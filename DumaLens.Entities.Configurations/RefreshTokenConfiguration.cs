using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="RefreshToken"/>
/// </summary>
/// <remarks>
/// Один активный refresh-токен на пользователя: уникальный индекс UserId с фильтром по RevokedAt (07-security.md, §2.1).
/// </remarks>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(RefreshTokenValidationConstants.TokenHashMaxLength)
            .IsRequired();
        builder.Property(x => x.SecurityStamp)
            .IsRequired();

        builder.HasIndex(x => x.TokenHash, $"IX_{nameof(RefreshToken)}_{nameof(RefreshToken.TokenHash)}")
            .IsUnique();
        builder.HasIndex(x => x.UserId, $"IX_{nameof(RefreshToken)}_{nameof(RefreshToken.UserId)}")
            .IsUnique()
            .HasFilter($"\"{nameof(RefreshToken.RevokedAt)}\" IS NULL");

        // Ссылочная целостность: токен не может указывать на несуществующего пользователя;
        // при будущем физическом удалении аккаунта его токены удаляются каскадом
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}