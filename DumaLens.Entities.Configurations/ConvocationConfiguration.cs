using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="Convocation"/>
/// </summary>
public class ConvocationConfiguration : IEntityTypeConfiguration<Convocation>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<Convocation> builder)
    {
        builder.ToTable("Convocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SiteCode)
            .HasMaxLength(ConvocationValidationConstants.SiteCodeMaxLength)
            .IsRequired();
        builder.Property(x => x.Title)
            .HasMaxLength(CommonValidationConstants.TitleMaxLength)
            .IsRequired();

        builder.HasIndex(x => x.Number, $"IX_{nameof(Convocation)}_{nameof(Convocation.Number)}")
            .IsUnique()
            .HasFilter($"\"{nameof(Convocation.DeletedAt)}\" IS NULL");
        builder.HasIndex(x => x.SiteCode, $"IX_{nameof(Convocation)}_{nameof(Convocation.SiteCode)}")
            .IsUnique()
            .HasFilter($"\"{nameof(Convocation.DeletedAt)}\" IS NULL");

        builder.HasData(ConvocationsDefault.Entities);
    }
}