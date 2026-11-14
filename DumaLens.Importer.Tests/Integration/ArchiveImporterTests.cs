using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Importer.Import;
using DumaLens.Importer.Tests.Infrastructure;
using DumaLens.Importer.Verify;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace DumaLens.Importer.Tests.Integration;

/// <summary>
/// Интеграционные тесты импорта мини-архива: фикстуры Э2 упаковываются во временный ZIP,
/// каждый тест получает одноразовую базу PostgreSQL (ADR-3)
/// </summary>
public sealed class ArchiveImporterTests
{
    private static readonly string[] FixtureNames = ["archive_126282", "archive_18642", "archive_empty_17470", "archive_plus0400"];

    /// <summary>
    /// Импорт мини-архива: голосования, индивидуальные голоса через COPY и справочники на лету
    /// </summary>
    [SkippableFact]
    public async Task Import_MiniArchive_ImportsVotesAndBuildsReferences()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false);

            summary.TotalFiles.Should().Be(4);
            summary.Imported.Should().Be(4);
            summary.Skipped.Should().Be(0);
            summary.Quarantined.Should().Be(0);
            summary.DeputyVotesImported.Should().Be(1340, "447 + 444 + 0 + 449 голосов в фикстурах");

            await using var context = database.CreateContext();
            var votes = await context.Set<Vote>().AsNoTracking().ToListAsync();
            votes.Should().HaveCount(4);
            votes.Should().OnlyContain(x => x.Source == VoteSource.Archive);

            var thirdReading = votes.Single(x => x.SiteId == 126282);
            thirdReading.HasResults.Should().BeTrue();
            thirdReading.Stage.Should().Be(VoteStage.ThirdReading);
            thirdReading.StageRaw.Should().Be("третье чтение");
            thirdReading.AcceptCountTotal.Should().Be(396);
            thirdReading.NoneCountTotal.Should().Be(51);
            thirdReading.BillId.Should().NotBeNull();

            var empty = votes.Single(x => x.SiteId == 17470);
            empty.HasResults.Should().BeFalse("78 пустых голосований архива — факт, не карантин");

            var deputyVotes = await context.Set<DeputyVote>().AsNoTracking().ToListAsync();
            deputyVotes.Should().HaveCount(1340);
            deputyVotes.Should().OnlyContain(x => x.FactionPeriodId != null);
            deputyVotes.Count(x => x.Result == VoteResult.Accept).Should().Be(776);
            deputyVotes.Count(x => x.Result == VoteResult.None).Should().Be(542);

            var bills = await context.Set<Bill>().AsNoTracking().ToListAsync();
            bills.Should().ContainSingle(x => x.Number == "500091-8");

            var deputies = await context.Set<Deputy>().AsNoTracking().CountAsync();
            deputies.Should().BeGreaterThan(0);

            var memberships = await context.Set<DeputyFactionMembership>().AsNoTracking().ToListAsync();
            memberships.Should().NotBeEmpty();
            memberships.Should().OnlyContain(x => x.DateFrom <= x.DateTo);

            var convocationNumbers = await context.Set<Convocation>().AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.Number);
            convocationNumbers[thirdReading.ConvocationId].Should().Be(8);
            convocationNumbers[votes.Single(x => x.SiteId == 18642).ConvocationId].Should().Be(1);
            convocationNumbers[empty.ConvocationId].Should().Be(3);
            convocationNumbers[votes.Single(x => x.SiteId == 87506).ConvocationId].Should().Be(6);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Повторный полный импорт не меняет данные (П-2): суммы и справочники идентичны
    /// </summary>
    [SkippableFact]
    public async Task Import_SecondFullRun_IsIdempotent()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            var before = await CountDatabase(database);

            var second = await RunImport(database.ConnectionString, zipPath, resume: false);
            second.Imported.Should().Be(4);
            second.DeputyVotesImported.Should().Be(1340);

            var after = await CountDatabase(database);
            after.Should().BeEquivalentTo(before);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Режим --resume пропускает уже загруженные голосования и не создаёт дубликатов (НФТ-04)
    /// </summary>
    [SkippableFact]
    public async Task Import_Resume_SkipsProcessedVotes()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            await RunImport(database.ConnectionString, zipPath, resume: false);
            var before = await CountDatabase(database);

            var resumed = await RunImport(database.ConnectionString, zipPath, resume: true);
            resumed.Skipped.Should().Be(4);
            resumed.Imported.Should().Be(0);
            resumed.DeputyVotesImported.Should().Be(0);

            var after = await CountDatabase(database);
            after.Should().BeEquivalentTo(before);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Пустой заголовок — карантин; повторный импорт не дублирует карантин;
    /// сверка учитывает карантин (база + карантин = паспорт)
    /// </summary>
    [SkippableFact]
    public async Task Import_EmptyTitle_QuarantinedOnceAndAccountedInVerify()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var (_, sourceJson) = FixtureEntry("archive_126282");
        var node = JsonNode.Parse(sourceJson)!;
        node["id"] = "999999";
        node["title"] = string.Empty;
        var entries = FixtureNames.Select(FixtureEntry).ToList();
        entries.Add(("vote-master/poll/2024-03-13/999999.json", node.ToJsonString()));
        var zipPath = CreateZip(entries.ToArray());
        try
        {
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false);
            summary.Imported.Should().Be(4);
            summary.Quarantined.Should().Be(1);
            summary.DeputyVotesImported.Should().Be(1340);

            await using (var context = database.CreateContext())
            {
                var quarantine = await context.Set<ImportQuarantine>().AsNoTracking().SingleAsync();
                quarantine.SourceFile.Should().Be("vote-master/poll/2024-03-13/999999.json");
                quarantine.SiteId.Should().Be(999999);
                quarantine.Reason.Should().Contain("EmptyTitle");
            }

            var report = await RunVerify(database.ConnectionString);
            report.DbVotes.Should().Be(4);
            report.QuarantineVotes.Should().Be(1);
            report.QuarantineDeputyVotes.Should().Be(447);
            report.Rows.Single(x => x.Name == "Всего голосований").Actual.Should().Be(5);
            report.Rows.Single(x => x.Name == "Всего индивидуальных голосов").Actual.Should().Be(1787);

            var second = await RunImport(database.ConnectionString, zipPath, resume: false);
            second.Quarantined.Should().Be(0, "карантин идемпотентен по имени файла");
            second.Skipped.Should().Be(1);

            await using var finalContext = database.CreateContext();
            (await finalContext.Set<ImportQuarantine>().CountAsync()).Should().Be(1);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Регресс: справочник, созданный в пачке (Added) и обновлённый в той же пачке,
    /// должен вставляться, а не обновлять несуществующую строку (DbUpdateConcurrencyException)
    /// </summary>
    [SkippableFact]
    public async Task Import_ReferenceCreatedAndUpdatedInSameBatch_InsertsCorrectly()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var (_, sourceJson) = FixtureEntry("archive_126282");
        var node = JsonNode.Parse(sourceJson)!;
        node["id"] = "999998";
        node["datetime"] = "2024-03-14T15:01:13+0300";
        var deputyNode = node["votes"]![0]!["deputy"]!;
        var deputySiteId = long.Parse(deputyNode["id"]!.ToString());
        var renamedFullName = deputyNode["name"]!.GetValue<string>() + " (тест)";
        deputyNode["name"] = renamedFullName;
        var entries = new[]
        {
            FixtureEntry("archive_126282"),
            ("vote-master/poll/2024-03-14/999998.json", node.ToJsonString()),
        };
        var zipPath = CreateZip(entries);
        try
        {
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false);
            summary.Imported.Should().Be(2);
            summary.Quarantined.Should().Be(0);
            summary.DeputyVotesImported.Should().Be(894);

            await using var context = database.CreateContext();
            (await context.Set<Vote>().CountAsync()).Should().Be(2);
            (await context.Set<DeputyVote>().CountAsync()).Should().Be(894);

            var renamed = await context.Set<Deputy>().AsNoTracking().SingleAsync(x => x.SiteId == deputySiteId);
            renamed.FullName.Should().Be(renamedFullName, "имя депутата уточняется последним голосованием пачки");

            var memberships = await context.Set<DeputyFactionMembership>().AsNoTracking()
                .Where(x => x.DeputyId == renamed.Id)
                .ToListAsync();
            memberships.Should().NotBeEmpty();
            memberships.Should().OnlyContain(x => x.DateFrom <= x.DateTo);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    private static string LoadFixture(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "json", $"{name}.json"));
    }

    private static (string EntryName, string Json) FixtureEntry(string name)
    {
        var json = LoadFixture(name);
        var node = JsonNode.Parse(json)!;
        var id = node["id"]!.ToString();
        var date = node["datetime"]!.GetValue<string>()[..10];
        return ($"vote-master/poll/{date}/{id}.json", json);
    }

    private static string CreateMiniZip()
    {
        return CreateZip(FixtureNames.Select(FixtureEntry).ToArray());
    }

    private static string CreateZip(params (string EntryName, string Json)[] entries)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dumalens_import_test_{Guid.NewGuid():N}.zip");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (entryName, json) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(json);
        }

        return path;
    }

    private static async Task<ImportSummary> RunImport(string connectionString, string zipPath, bool resume)
    {
        await using var provider = ImporterHost.BuildServiceProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<ArchiveImporter>();
        var options = new ImportOptions { ZipPath = zipPath, BatchSize = 2, Resume = resume };
        return await importer.Import(options, CancellationToken.None);
    }

    private static async Task<VerifyReport> RunVerify(string connectionString)
    {
        await using var provider = ImporterHost.BuildServiceProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        var verifier = scope.ServiceProvider.GetRequiredService<ArchiveVerifier>();
        return await verifier.Verify(CancellationToken.None);
    }

    private static async Task<DbCounts> CountDatabase(DatabaseFixture database)
    {
        await using var context = database.CreateContext();
        return new DbCounts(
            await context.Set<Vote>().CountAsync(),
            await context.Set<DeputyVote>().CountAsync(),
            await context.Set<Deputy>().CountAsync(),
            await context.Set<FactionPeriod>().CountAsync(),
            await context.Set<DeputyFactionMembership>().CountAsync(),
            await context.Set<Bill>().CountAsync(),
            await context.Set<ImportQuarantine>().CountAsync());
    }

    private sealed record DbCounts(long Votes, long DeputyVotes, long Deputies, long Periods, long Memberships, long Bills, long Quarantine);
}