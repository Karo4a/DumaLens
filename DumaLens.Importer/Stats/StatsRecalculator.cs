using DumaLens.Common.Contracts;
using DumaLens.Context;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Importer.Import;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Importer.Stats;

/// <summary>
/// Полный пересчёт фракционных агрегатов VoteFactionStats из индивидуальных голосов (Э4).
/// Set-based запрос вместо построчного чтения: на полном архиве (~45,6 млн голосов) пересчёт
/// выполняется за минуты. Формулы и округление (ROUND до <see cref="CommonValidationConstants.PercentScale"/>
/// знаков, половина — от нуля) совпадают с <see cref="Services.Contracts.IVoteFactionStatsService"/>,
/// строящим агрегаты при импорте; идентичность результатов закреплена интеграционным тестом.
/// </summary>
public sealed class StatsRecalculator
{
    private const string DeleteStatsSql = "DELETE FROM \"VoteFactionStats\"";

    private static readonly string InsertStatsSql = $$"""
        INSERT INTO "VoteFactionStats" (
            "VoteId", "FactionPeriodId", "FactionId",
            "AcceptCount", "DeclineCount", "AbstainCount", "NoneCount", "MemberCount", "VotedCount",
            "AcceptPctMembers", "DeclinePctMembers", "AbstainPctMembers", "NonePctMembers",
            "AcceptPctVoted", "DeclinePctVoted", "AbstainPctVoted")
        SELECT
            v."Id",
            fp."Id",
            fp."FactionId",
            COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Accept}}),
            COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Decline}}),
            COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Abstain}}),
            COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.None}}),
            COUNT(*),
            COUNT(*) FILTER (WHERE dv."Result" <> {{(int)VoteResult.None}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Accept}}) / NULLIF(COUNT(*), 0), {{CommonValidationConstants.PercentScale}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Decline}}) / NULLIF(COUNT(*), 0), {{CommonValidationConstants.PercentScale}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Abstain}}) / NULLIF(COUNT(*), 0), {{CommonValidationConstants.PercentScale}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.None}}) / NULLIF(COUNT(*), 0), {{CommonValidationConstants.PercentScale}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Accept}}) / NULLIF(COUNT(*) FILTER (WHERE dv."Result" <> {{(int)VoteResult.None}}), 0), {{CommonValidationConstants.PercentScale}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Decline}}) / NULLIF(COUNT(*) FILTER (WHERE dv."Result" <> {{(int)VoteResult.None}}), 0), {{CommonValidationConstants.PercentScale}}),
            ROUND(100.0 * COUNT(*) FILTER (WHERE dv."Result" = {{(int)VoteResult.Abstain}}) / NULLIF(COUNT(*) FILTER (WHERE dv."Result" <> {{(int)VoteResult.None}}), 0), {{CommonValidationConstants.PercentScale}})
        FROM "DeputyVotes" dv
        JOIN "FactionPeriods" fp ON fp."RowId" = dv."FactionPeriodId"
        JOIN "Votes" v ON v."RowId" = dv."VoteId"
        WHERE dv."FactionPeriodId" IS NOT NULL AND fp."DeletedAt" IS NULL AND v."DeletedAt" IS NULL
        GROUP BY v."Id", fp."Id", fp."FactionId"
        """;

    private readonly DumaLensContext context;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ImportLog log;

    /// <summary>
    /// Конструктор
    /// </summary>
    public StatsRecalculator(DumaLensContext context, IDateTimeProvider dateTimeProvider, ImportLog log)
    {
        this.context = context;
        this.dateTimeProvider = dateTimeProvider;
        this.log = log;
    }

    /// <summary>
    /// Пересчитать все агрегаты в одной транзакции: старые строки удаляются, новые строятся
    /// из текущих индивидуальных голосов. Голосования и периоды фракций, помеченные удалёнными,
    /// в статистику не попадают. Повторный запуск идемпотентен.
    /// </summary>
    public async Task<RecalcSummary> Recalc(CancellationToken cancellationToken)
    {
        var startedAt = dateTimeProvider.UtcNow();
        await context.Database.MigrateAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        // DELETE вместо TRUNCATE: ACCESS EXCLUSIVE блокировал бы чтение агрегатов на весь пересчёт (НФТ-11)
        await context.Database.ExecuteSqlRawAsync(DeleteStatsSql, cancellationToken);
        var rowsRebuilt = await context.Database.ExecuteSqlRawAsync(InsertStatsSql, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var summary = new RecalcSummary
        {
            RowsRebuilt = rowsRebuilt,
            Elapsed = dateTimeProvider.UtcNow() - startedAt,
        };
        log.Info($"Пересчёт фракционных агрегатов завершён за {summary.Elapsed:hh\\:mm\\:ss}: построено строк {summary.RowsRebuilt:N0}");
        return summary;
    }
}