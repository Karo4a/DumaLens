using DumaLens.Entities;
using DumaLens.Entities.ValidationConstants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace DumaLens.Entities.Configurations;

/// <summary>
/// Описывает конфигурацию <see cref="Vote"/>
/// </summary>
/// <remarks>
/// Title — text без ограничения длины (паспорт данных, 02-data-sources.md, §2.3.9);
/// TitleSearch — генерируемая tsvector-колонка с GIN-индексом для полнотекстового поиска (ADR-1).
/// </remarks>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>, IEntityConfiguration
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}.Configure"/>
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("Votes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired();
        builder.Property<NpgsqlTsVector>("TitleSearch")
            .HasColumnType("tsvector")
            .HasComputedColumnSql("to_tsvector('russian'::regconfig, \"Title\")", stored: true)
            .ValueGeneratedOnAddOrUpdate();
        builder.Property(x => x.Stage)
            .HasConversion<string>()
            .HasMaxLength(VoteValidationConstants.StageMaxLength);
        builder.Property(x => x.StageRaw)
            .HasMaxLength(VoteValidationConstants.StageRawMaxLength);
        builder.Property(x => x.Outcome)
            .HasConversion<string>()
            .HasMaxLength(VoteValidationConstants.OutcomeMaxLength);
        builder.Property(x => x.AcceptPercentSite)
            .HasColumnType(CommonValidationConstants.PercentColumnType);
        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(VoteValidationConstants.SourceMaxLength);

        builder.HasOne(x => x.Convocation)
            .WithMany()
            .HasForeignKey(x => x.ConvocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bill)
            .WithMany()
            .HasForeignKey(x => x.BillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SiteId, $"IX_{nameof(Vote)}_{nameof(Vote.SiteId)}")
            .IsUnique()
            .HasFilter($"\"{nameof(Vote.DeletedAt)}\" IS NULL");
        builder.HasIndex(new[] { "TitleSearch" })
            .HasDatabaseName($"IX_{nameof(Vote)}_TitleSearch")
            .HasMethod("gin");
        builder.HasIndex(x => x.VotedAt, $"IX_{nameof(Vote)}_{nameof(Vote.VotedAt)}");
        builder.HasIndex(x => x.ConvocationId, $"IX_{nameof(Vote)}_{nameof(Vote.ConvocationId)}");
        builder.HasIndex(x => x.BillId, $"IX_{nameof(Vote)}_{nameof(Vote.BillId)}");
        builder.HasIndex(x => x.Stage, $"IX_{nameof(Vote)}_{nameof(Vote.Stage)}");
        builder.HasIndex(x => new { x.ConvocationId, x.VotedAt }, $"IX_{nameof(Vote)}_{nameof(Vote.ConvocationId)}_{nameof(Vote.VotedAt)}");
    }
}