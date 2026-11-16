using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Services.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace DumaLens.Importer.Import;

/// <summary>
/// Массовая запись индивидуальных голосов и фракционных агрегатов через COPY FORMAT BINARY
/// (общая для импорта архива и кроулера живого сайта)
/// </summary>
/// <remarks>
/// Строки перед COPY сортируются по ключам для почти последовательных вставок в индексы
/// (замер: ~1,4× к скорости COPY). Узкие ключи справочников (RowId) должны быть уже
/// материализованы базой — вызов после SaveChanges пачки.
/// </remarks>
public static class BulkCopyWriter
{
    private const string CopyCommand = "COPY \"DeputyVotes\" (\"VoteId\", \"DeputyId\", \"FactionPeriodId\", \"Result\") FROM STDIN (FORMAT BINARY)";
    private const string CopyStatsCommand = "COPY \"VoteFactionStats\" (\"VoteId\", \"FactionPeriodId\", \"FactionId\", \"AcceptCount\", \"DeclineCount\", \"AbstainCount\", \"NoneCount\", \"MemberCount\", \"VotedCount\", \"AcceptPctMembers\", \"DeclinePctMembers\", \"AbstainPctMembers\", \"NonePctMembers\", \"AcceptPctVoted\", \"DeclinePctVoted\", \"AbstainPctVoted\") FROM STDIN (FORMAT BINARY)";

    /// <summary>
    /// Материализация индивидуальных голосов с узкими ключами справочников после SaveChanges пачки
    /// </summary>
    public static List<DeputyVote> MaterializeDeputyVotes(List<DeputyVoteLink> pending)
    {
        var deputyVotes = new List<DeputyVote>(pending.Count);
        foreach (var row in pending)
        {
            deputyVotes.Add(new DeputyVote
            {
                VoteId = row.Vote.RowId,
                DeputyId = row.Deputy.RowId,
                FactionPeriodId = row.Period?.RowId,
                Result = row.Result,
            });
        }

        return deputyVotes;
    }

    /// <summary>
    /// Строит фракционные агрегаты пачки из индивидуальных голосов (формулы — в сервисе, Э4)
    /// </summary>
    public static List<VoteFactionStats> BuildFactionStats(
        List<DeputyVote> deputyVotes,
        List<DeputyVoteLink> pending,
        IReadOnlyDictionary<short, FactionPeriodStatsKey> periodKeys,
        IVoteFactionStatsService statsService)
    {
        if (deputyVotes.Count == 0)
        {
            return [];
        }

        var voteIdsByRowId = pending.Select(x => x.Vote).Distinct().ToDictionary(x => x.RowId, x => x.Id);
        var statsRows = new List<VoteFactionStats>();
        foreach (var voteGroup in deputyVotes.GroupBy(x => x.VoteId))
        {
            statsRows.AddRange(statsService.BuildStats(voteIdsByRowId[voteGroup.Key], voteGroup.ToList(), periodKeys));
        }

        return statsRows;
    }

    /// <summary>
    /// Сортировка строк по ключам перед COPY: вставки в индексы становятся почти последовательными
    /// </summary>
    public static void SortForCopy(List<DeputyVote> deputyVotes, List<VoteFactionStats> statsRows)
    {
        deputyVotes.Sort(static (a, b) =>
        {
            var byVote = a.VoteId.CompareTo(b.VoteId);
            return byVote != 0 ? byVote : a.DeputyId.CompareTo(b.DeputyId);
        });
        statsRows.Sort(static (a, b) =>
        {
            var byVote = a.VoteId.CompareTo(b.VoteId);
            return byVote != 0 ? byVote : a.FactionPeriodId.CompareTo(b.FactionPeriodId);
        });
    }

    /// <summary>
    /// COPY индивидуальных голосов (составной ключ, без Id и аудита — ADR-2)
    /// </summary>
    public static async Task CopyDeputyVotes(NpgsqlConnection connection, List<DeputyVote> rows, CancellationToken cancellationToken)
    {
        await using var importer = await connection.BeginBinaryImportAsync(CopyCommand, cancellationToken);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(row.VoteId, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(row.DeputyId, NpgsqlDbType.Integer, cancellationToken);
            if (row.FactionPeriodId.HasValue)
            {
                await importer.WriteAsync(row.FactionPeriodId.Value, NpgsqlDbType.Smallint, cancellationToken);
            }
            else
            {
                await importer.WriteNullAsync(cancellationToken);
            }

            await importer.WriteAsync((short)row.Result, NpgsqlDbType.Smallint, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    /// <summary>
    /// COPY фракционных агрегатов (16 колонок, проценты numeric с null при нулевом знаменателе)
    /// </summary>
    public static async Task CopyFactionStats(NpgsqlConnection connection, List<VoteFactionStats> rows, CancellationToken cancellationToken)
    {
        await using var importer = await connection.BeginBinaryImportAsync(CopyStatsCommand, cancellationToken);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(row.VoteId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.FactionPeriodId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.FactionId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.AcceptCount, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(row.DeclineCount, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(row.AbstainCount, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(row.NoneCount, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(row.MemberCount, NpgsqlDbType.Integer, cancellationToken);
            await importer.WriteAsync(row.VotedCount, NpgsqlDbType.Integer, cancellationToken);
            await WritePercent(importer, row.AcceptPctMembers, cancellationToken);
            await WritePercent(importer, row.DeclinePctMembers, cancellationToken);
            await WritePercent(importer, row.AbstainPctMembers, cancellationToken);
            await WritePercent(importer, row.NonePctMembers, cancellationToken);
            await WritePercent(importer, row.AcceptPctVoted, cancellationToken);
            await WritePercent(importer, row.DeclinePctVoted, cancellationToken);
            await WritePercent(importer, row.AbstainPctVoted, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private static async Task WritePercent(NpgsqlBinaryImporter importer, decimal? value, CancellationToken cancellationToken)
    {
        if (value.HasValue)
        {
            await importer.WriteAsync(value.Value, NpgsqlDbType.Numeric, cancellationToken);
        }
        else
        {
            await importer.WriteNullAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Голос с ссылками на сущности справочников: узкие ключи проставляются после SaveChanges пачки
/// </summary>
public sealed record DeputyVoteLink(Vote Vote, Deputy Deputy, FactionPeriod? Period, VoteResult Result);