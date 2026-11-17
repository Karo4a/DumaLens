using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="FactionPeriod"/>
/// </summary>
public class FactionPeriodConfiguration : IEntityTypeConfiguration<FactionPeriod>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<FactionPeriod> builder)
    {
        builder.ToTable("FactionPeriods");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowId)
            .ValueGeneratedOnAdd();
        // Уникальность RowId обеспечивает альтернативный ключ (HasPrincipalKey в DeputyVoteConfiguration);
        // отдельный уникальный индекс не создаём — FK опирается на индекс AK

        builder.Property(x => x.Title)
            .HasMaxLength(CommonValidationConstants.TitleMaxLength)
            .IsRequired();

        builder.HasOne(x => x.Faction)
            .WithMany()
            .HasForeignKey(x => x.FactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Convocation)
            .WithMany()
            .HasForeignKey(x => x.ConvocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SiteId, x.ConvocationId }, $"IX_{nameof(FactionPeriod)}_{nameof(FactionPeriod.SiteId)}_{nameof(FactionPeriod.ConvocationId)}")
            .IsUnique()
            .HasFilter($"\"{nameof(FactionPeriod.DeletedAt)}\" IS NULL");
        builder.HasIndex(x => x.FactionId, $"IX_{nameof(FactionPeriod)}_{nameof(FactionPeriod.FactionId)}");

        builder.HasData(FactionPeriodsDefault.Entities);
    }
}