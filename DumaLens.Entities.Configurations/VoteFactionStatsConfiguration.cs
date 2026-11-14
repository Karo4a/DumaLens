using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="VoteFactionStats"/>
/// </summary>
/// <remarks>
/// Потоковая таблица: составной ключ (VoteId, FactionPeriodId), без аудита и soft-delete (ADR-2, ADR-7).
/// Проценты — numeric(5,2), индексы под фильтры фракционного сравнения по каждой базе (04-functionality.md, §3).
/// </remarks>
public class VoteFactionStatsConfiguration : IEntityTypeConfiguration<VoteFactionStats>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<VoteFactionStats> builder)
    {
        builder.ToTable("VoteFactionStats");
        builder.HasKey(x => new { x.VoteId, x.FactionPeriodId });

        builder.Property(x => x.AcceptPctMembers)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.DeclinePctMembers)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.AbstainPctMembers)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.NonePctMembers)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.AcceptPctVoted)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.DeclinePctVoted)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.AbstainPctVoted)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.NonePctVoted)
            .HasColumnType(CommonValidationConstants.PercentColumnType);

        builder.HasOne(x => x.Vote)
            .WithMany()
            .HasForeignKey(x => x.VoteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FactionPeriod)
            .WithMany()
            .HasForeignKey(x => x.FactionPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Faction)
            .WithMany()
            .HasForeignKey(x => x.FactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FactionId, x.AcceptPctMembers }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.AcceptPctMembers)}");
        builder.HasIndex(x => new { x.FactionId, x.DeclinePctMembers }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.DeclinePctMembers)}");
        builder.HasIndex(x => new { x.FactionId, x.AbstainPctMembers }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.AbstainPctMembers)}");
        builder.HasIndex(x => new { x.FactionId, x.NonePctMembers }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.NonePctMembers)}");
        builder.HasIndex(x => new { x.FactionId, x.AcceptPctVoted }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.AcceptPctVoted)}");
        builder.HasIndex(x => new { x.FactionId, x.DeclinePctVoted }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.DeclinePctVoted)}");
        builder.HasIndex(x => new { x.FactionId, x.AbstainPctVoted }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.AbstainPctVoted)}");
        builder.HasIndex(x => new { x.FactionId, x.NonePctVoted }, $"IX_{nameof(VoteFactionStats)}_{nameof(VoteFactionStats.FactionId)}_{nameof(VoteFactionStats.NonePctVoted)}");
    }
}