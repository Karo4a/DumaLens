using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="Bill"/>
/// </summary>
public class BillConfiguration : IEntityTypeConfiguration<Bill>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("Bills");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .HasMaxLength(BillValidationConstants.NumberMaxLength)
            .IsRequired();

        builder.Ignore(x => x.SozdUrl);

        builder.HasIndex(x => x.Number, $"IX_{nameof(Bill)}_{nameof(Bill.Number)}")
            .IsUnique()
            .HasFilter($"\"{nameof(Bill.DeletedAt)}\" IS NULL");
    }
}