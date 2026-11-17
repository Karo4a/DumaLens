using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="Deputy"/>
/// </summary>
public class DeputyConfiguration : IEntityTypeConfiguration<Deputy>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<Deputy> builder)
    {
        builder.ToTable("Deputies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowId)
            .ValueGeneratedOnAdd();
        // Уникальность RowId обеспечивает альтернативный ключ (HasPrincipalKey в DeputyVoteConfiguration);
        // отдельный уникальный индекс не создаём — FK опирается на индекс AK

        builder.Property(x => x.FullName)
            .HasMaxLength(DeputyValidationConstants.FullNameMaxLength)
            .IsRequired();
        builder.Property(x => x.SortName)
            .HasMaxLength(DeputyValidationConstants.SortNameMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.SiteId, $"IX_{nameof(Deputy)}_{nameof(Deputy.SiteId)}")
            .IsUnique()
            .HasFilter($"\"{nameof(Deputy.DeletedAt)}\" IS NULL");
        builder.HasIndex(x => x.FullName, $"IX_{nameof(Deputy)}_{nameof(Deputy.FullName)}");
    }
}