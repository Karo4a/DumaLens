using DumaLens.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="DeputyFactionMembership"/>
/// </summary>
public class DeputyFactionMembershipConfiguration : IEntityTypeConfiguration<DeputyFactionMembership>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<DeputyFactionMembership> builder)
    {
        builder.ToTable("DeputyFactionMemberships");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Deputy)
            .WithMany()
            .HasForeignKey(x => x.DeputyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FactionPeriod)
            .WithMany()
            .HasForeignKey(x => x.FactionPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DeputyId, x.FactionPeriodId }, $"IX_{nameof(DeputyFactionMembership)}_{nameof(DeputyFactionMembership.DeputyId)}_{nameof(DeputyFactionMembership.FactionPeriodId)}");
    }
}