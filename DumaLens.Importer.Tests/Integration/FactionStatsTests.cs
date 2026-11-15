using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Importer.Import;
using DumaLens.Importer.Stats;
using DumaLens.Importer.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace DumaLens.Importer.Tests.Integration;

/// <summary>
/// Интеграционные тесты фракционных агрегатов (Э4): построение при импорте,
/// идентичность формул импорта и recalc-stats, идемпотентность пересчёта,
/// исключение удалённых голосований и обновление после исправления данных
/// </summary>
public sealed class FactionStatsTests
{
    private static readonly string[] FixtureNames = ["archive_126282", "archive_18642", "archive_empty_17470", "archive_plus0400"];

    /// <summary>
    /// Импорт строит агрегаты в той же транзакции, что и индивидуальные голоса:
    /// суммы совпадают с контрольными суммами голосований, проценты в диапазоне,
    /// null — только при нулевом числе голосовавших
    /// </summary>
    [SkippableFact]
    public async Task Import_MiniArchive_BuildsFactionStats()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false);
            summary.FactionStatsImported.Should().Be(20, "6 + 10 + 0 + 4 фракций в фикстурах");

            await using var context = database.CreateContext();
            var stats = await context.Set<VoteFactionStats>().AsNoTracking().ToListAsync();
            stats.Should().HaveCount(20);

            var votes = await context.Set<Vote>().AsNoTracking().ToDictionaryAsync(x => x.SiteId);
            stats.Sum(x => x.AcceptCount).Should().Be(votes.Values.Sum(x => x.AcceptCountTotal));
            stats.Sum(x => x.DeclineCount).Should().Be(votes.Values.Sum(x => x.DeclineCountTotal));
            stats.Sum(x => x.AbstainCount).Should().Be(votes.Values.Sum(x => x.AbstainCountTotal));
            stats.Sum(x => x.NoneCount).Should().Be(votes.Values.Sum(x => x.NoneCountTotal));
            stats.Sum(x => x.MemberCount).Should().Be(1340, "все депутаты фикстур состоят во фракциях");
            stats.Sum(x => x.VotedCount).Should().Be(1340 - 542);

            var periodIds = await context.Set<FactionPeriod>().AsNoTracking().Select(x => x.Id).ToListAsync();
            stats.Should().OnlyContain(x => periodIds.Contains(x.FactionPeriodId));
            stats.Should().NotContain(x => x.VoteId == votes[17470].Id, "пустое голосование не даёт строк статистики");

            stats.Where(x => x.VotedCount == 0).Should().OnlyContain(x =>
                x.AcceptPctVoted == null && x.DeclinePctVoted == null && x.AbstainPctVoted == null);
            stats.Where(x => x.VotedCount > 0).Should().OnlyContain(x =>
                x.AcceptPctVoted != null && x.DeclinePctVoted != null && x.AbstainPctVoted != null);
            stats.Should().OnlyContain(x =>
                x.AcceptPctMembers != null && x.DeclinePctMembers != null && x.AbstainPctMembers != null && x.NonePctMembers != null);
            foreach (var row in stats)
            {
                AssertPercentRange(row);
            }

            var er = await ReadErStats(database);
            er.AcceptCount.Should().Be(289);
            er.NoneCount.Should().Be(35);
            er.MemberCount.Should().Be(324);
            er.VotedCount.Should().Be(289);
            er.AcceptPctMembers.Should().Be(89.20m);
            er.NonePctMembers.Should().Be(10.80m);
            er.AcceptPctVoted.Should().Be(100.00m);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// recalc-stats после импорта строит те же агрегаты: формулы SQL-пересчёта
    /// идентичны формулам сервиса, строящего статистику при импорте
    /// </summary>
    [SkippableFact]
    public async Task Recalc_AfterImport_ProducesIdenticalStats()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            var before = await ReadStats(database);

            var summary = await RunRecalc(database.ConnectionString);

            summary.RowsRebuilt.Should().Be(20);
            var after = await ReadStats(database);
            after.Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Повторный recalc-stats идемпотентен: состав и значения строк не меняются
    /// </summary>
    [SkippableFact]
    public async Task Recalc_SecondRun_IsIdempotent()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            var first = await RunRecalc(database.ConnectionString);
            var afterFirst = await ReadStats(database);

            var second = await RunRecalc(database.ConnectionString);

            second.RowsRebuilt.Should().Be(first.RowsRebuilt);
            var afterSecond = await ReadStats(database);
            afterSecond.Should().BeEquivalentTo(afterFirst, options => options.WithStrictOrdering());
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// После исправления индивидуального голоса пересчёт обновляет агрегат:
    /// 289 «за» ЕР в голосовании 126282 → 288 «за» + 1 «против»
    /// </summary>
    [SkippableFact]
    public async Task Recalc_AfterDeputyVoteFix_UpdatesStats()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            await using (var context = database.CreateContext())
            {
                var vote = await context.Set<Vote>().SingleAsync(x => x.SiteId == 126282);
                var periodId = await context.Set<FactionPeriod>()
                    .Where(x => x.SiteId == 72100024 && x.ConvocationId == vote.ConvocationId)
                    .Select(x => x.Id)
                    .SingleAsync();
                var deputyVote = await context.Set<DeputyVote>()
                    .FirstAsync(x => x.VoteId == vote.Id && x.FactionPeriodId == periodId && x.Result == VoteResult.Accept);
                deputyVote.Result = VoteResult.Decline;
                await context.SaveChangesAsync();
            }

            var summary = await RunRecalc(database.ConnectionString);

            summary.RowsRebuilt.Should().Be(20);
            var er = await ReadErStats(database);
            er.AcceptCount.Should().Be(288);
            er.DeclineCount.Should().Be(1);
            er.NoneCount.Should().Be(35);
            er.MemberCount.Should().Be(324);
            er.VotedCount.Should().Be(289);
            er.AcceptPctMembers.Should().Be(88.89m);
            er.DeclinePctMembers.Should().Be(0.31m);
            er.AcceptPctVoted.Should().Be(99.65m);
            er.DeclinePctVoted.Should().Be(0.35m);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Помеченное удалённым голосование не попадает в пересчитанную статистику
    /// </summary>
    [SkippableFact]
    public async Task Recalc_SoftDeletedVote_ExcludedFromStats()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            await using (var context = database.CreateContext())
            {
                var vote = await context.Set<Vote>().SingleAsync(x => x.SiteId == 18642);
                vote.DeletedAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync();
            }

            var summary = await RunRecalc(database.ConnectionString);

            summary.RowsRebuilt.Should().Be(10, "10 строк голосования 18642 исключены");
            var stats = await ReadStats(database);
            stats.Should().HaveCount(10);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Пересчёт удаляет устаревшие строки: после удаления индивидуальных голосов
    /// голосования его агрегаты исчезают
    /// </summary>
    [SkippableFact]
    public async Task Recalc_RemovedDeputyVotes_RemovesStaleStats()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            await using (var context = database.CreateContext())
            {
                var vote = await context.Set<Vote>().SingleAsync(x => x.SiteId == 87506);
                await context.Set<DeputyVote>().Where(x => x.VoteId == vote.Id).ExecuteDeleteAsync();
            }

            var summary = await RunRecalc(database.ConnectionString);

            summary.RowsRebuilt.Should().Be(16, "4 строки голосования 87506 удалены");
            var stats = await ReadStats(database);
            stats.Should().HaveCount(16);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    private static async Task<List<VoteFactionStats>> ReadStats(DatabaseFixture database)
    {
        await using var context = database.CreateContext();
        return await context.Set<VoteFactionStats>().AsNoTracking()
            .OrderBy(x => x.VoteId).ThenBy(x => x.FactionPeriodId)
            .ToListAsync();
    }

    private static async Task<VoteFactionStats> ReadErStats(DatabaseFixture database)
    {
        await using var context = database.CreateContext();
        var vote = await context.Set<Vote>().AsNoTracking().SingleAsync(x => x.SiteId == 126282);
        var periodId = await context.Set<FactionPeriod>().AsNoTracking()
            .Where(x => x.SiteId == 72100024 && x.ConvocationId == vote.ConvocationId)
            .Select(x => x.Id)
            .SingleAsync();
        return await context.Set<VoteFactionStats>().AsNoTracking()
            .SingleAsync(x => x.VoteId == vote.Id && x.FactionPeriodId == periodId);
    }

    private static void AssertPercentRange(VoteFactionStats row)
    {
        // Все проценты — 0–100: от состава и от голосовавших (accept/decline/abstain);
        // «не голосовал» от голосовавших не хранится (04-functionality.md, §1.2)
        foreach (var percent in new[]
        {
            row.AcceptPctMembers, row.DeclinePctMembers, row.AbstainPctMembers, row.NonePctMembers,
            row.AcceptPctVoted, row.DeclinePctVoted, row.AbstainPctVoted,
        })
        {
            if (percent.HasValue)
            {
                percent.Value.Should().BeInRange(0m, 100m);
            }
        }
    }

    private static async Task<ImportSummary> RunImport(string connectionString, string zipPath, bool resume)
    {
        await using var provider = ImporterHost.BuildServiceProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<ArchiveImporter>();
        var options = new ImportOptions { ZipPath = zipPath, BatchSize = 2, Resume = resume };
        return await importer.Import(options, CancellationToken.None);
    }

    private static async Task<RecalcSummary> RunRecalc(string connectionString)
    {
        await using var provider = ImporterHost.BuildServiceProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        var recalculator = scope.ServiceProvider.GetRequiredService<StatsRecalculator>();
        return await recalculator.Recalc(CancellationToken.None);
    }

    private static (string EntryName, string Json) FixtureEntry(string name)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "json", $"{name}.json"));
        var node = JsonNode.Parse(json)!;
        var id = node["id"]!.ToString();
        var date = node["datetime"]!.GetValue<string>()[..10];
        return ($"vote-master/poll/{date}/{id}.json", json);
    }

    private static string CreateMiniZip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dumalens_stats_test_{Guid.NewGuid():N}.zip");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var name in FixtureNames)
        {
            var (entryName, json) = FixtureEntry(name);
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(json);
        }

        return path;
    }
}