using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="ImportQuarantine"/>
/// </summary>
public class ImportQuarantineConfiguration : IEntityTypeConfiguration<ImportQuarantine>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<ImportQuarantine> builder)
    {
        builder.ToTable("ImportQuarantine");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceFile)
            .HasMaxLength(ImportQuarantineValidationConstants.SourceFileMaxLength);
        builder.Property(x => x.SourceUrl)
            .HasMaxLength(ImportQuarantineValidationConstants.SourceUrlMaxLength);
        builder.Property(x => x.Reason)
            .HasMaxLength(ImportQuarantineValidationConstants.ReasonMaxLength)
            .IsRequired();
        builder.Property(x => x.RawJson)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAt, $"IX_{nameof(ImportQuarantine)}_{nameof(ImportQuarantine.CreatedAt)}");
    }
}