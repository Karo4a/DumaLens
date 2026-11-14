using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="Faction"/>
/// </summary>
public class FactionConfiguration : IEntityTypeConfiguration<Faction>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<Faction> builder)
    {
        builder.ToTable("Factions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(FactionValidationConstants.CodeMaxLength)
            .IsRequired();
        builder.Property(x => x.Title)
            .HasMaxLength(CommonValidationConstants.TitleMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.Code, $"IX_{nameof(Faction)}_{nameof(Faction.Code)}")
            .IsUnique()
            .HasFilter($"\"{nameof(Faction.DeletedAt)}\" IS NULL");

        builder.HasData(FactionsDefault.Entities);
    }
}