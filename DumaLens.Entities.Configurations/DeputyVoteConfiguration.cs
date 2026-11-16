using DumaLens.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="DeputyVote"/>
/// </summary>
/// <remarks>
/// Потоковая таблица: составной ключ (VoteId, DeputyId), без аудита и soft-delete (ADR-2).
/// </remarks>
public class DeputyVoteConfiguration : IEntityTypeConfiguration<DeputyVote>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<DeputyVote> builder)
    {
        builder.ToTable("DeputyVotes");
        builder.HasKey(x => new { x.VoteId, x.DeputyId });

        builder.Property(x => x.Result)
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.HasOne(x => x.Vote)
            .WithMany()
            .HasForeignKey(x => x.VoteId)
            .HasPrincipalKey(x => x.RowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Deputy)
            .WithMany()
            .HasForeignKey(x => x.DeputyId)
            .HasPrincipalKey(x => x.RowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FactionPeriod)
            .WithMany()
            .HasForeignKey(x => x.FactionPeriodId)
            .HasPrincipalKey(x => x.RowId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.FactionPeriodId)
            .HasColumnType("smallint");

        builder.HasIndex(x => x.DeputyId, $"IX_{nameof(DeputyVote)}_{nameof(DeputyVote.DeputyId)}");
        builder.HasIndex(x => new { x.VoteId, x.FactionPeriodId }, $"IX_{nameof(DeputyVote)}_{nameof(DeputyVote.VoteId)}_{nameof(DeputyVote.FactionPeriodId)}");
    }
}