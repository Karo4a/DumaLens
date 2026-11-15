using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Importer.Import;
using DumaLens.Importer.Tests.Infrastructure;
using DumaLens.Importer.Verify;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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
            summary.FactionStatsImported.Should().Be(20, "6 + 10 + 0 + 4 фракций в фикстурах");

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
            renamed.FullName.Should().Be(renamedFullName, "имя депутата уточняется по самому свежему голосованию");

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

    /// <summary>
    /// Регресс (Б2): реимпорт архива не затирает итог, процент и источник, полученные с сайта (ADR-9)
    /// </summary>
    [SkippableFact]
    public async Task Import_ExistingSiteVote_PreservesSiteFields()
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
                vote.Source = VoteSource.Site;
                vote.Outcome = VoteOutcome.Accepted;
                vote.AcceptPercentSite = 88.4m;
                await context.SaveChangesAsync();
            }

            await RunImport(database.ConnectionString, zipPath, resume: false);

            await using var finalContext = database.CreateContext();
            var preserved = await finalContext.Set<Vote>().AsNoTracking().SingleAsync(x => x.SiteId == 126282);
            preserved.Source.Should().Be(VoteSource.Site, "реимпорт архива не затирает источник сайта");
            preserved.Outcome.Should().Be(VoteOutcome.Accepted, "итог — факт сайта (ADR-9)");
            preserved.AcceptPercentSite.Should().Be(88.4m, "процент принятия сайта сохраняется");
            preserved.AcceptCountTotal.Should().Be(396, "суммы архива при этом обновляются");
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Регресс (Б1): членство с null-границами заполняется датами голосований при реимпорте
    /// </summary>
    [SkippableFact]
    public async Task Import_MembershipWithNullBounds_ExtendedByVotes()
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
                var membership = await context.Set<DeputyFactionMembership>().FirstAsync();
                membership.DateFrom = null;
                membership.DateTo = null;
                await context.SaveChangesAsync();
            }

            await RunImport(database.ConnectionString, zipPath, resume: false);

            await using var finalContext = database.CreateContext();
            var memberships = await finalContext.Set<DeputyFactionMembership>().AsNoTracking().ToListAsync();
            memberships.Should().OnlyContain(x => x.DateFrom != null && x.DateTo != null && x.DateFrom <= x.DateTo,
                "null-границы членства заполняются датами голосований");
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Регресс (Б3): файл, ранее забракованный и ставший годным, импортируется,
    /// карантин снимается и сверка не считает голосование дважды
    /// </summary>
    [SkippableFact]
    public async Task Import_PreviouslyQuarantinedFileNowValid_QuarantineCleared()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var (_, sourceJson) = FixtureEntry("archive_126282");
        var node = JsonNode.Parse(sourceJson)!;
        node["id"] = "999997";
        node["title"] = string.Empty;
        var invalidEntries = FixtureNames.Select(FixtureEntry).ToList();
        invalidEntries.Add(("vote-master/poll/2024-03-13/999997.json", node.ToJsonString()));
        var invalidZip = CreateZip(invalidEntries.ToArray());

        node["title"] = "(третье чтение) О проекте федерального закона № 500091-8 \"О внесении изменений в статью 12\"";
        var validEntries = FixtureNames.Select(FixtureEntry).ToList();
        validEntries.Add(("vote-master/poll/2024-03-13/999997.json", node.ToJsonString()));
        var validZip = CreateZip(validEntries.ToArray());
        try
        {
            var first = await RunImport(database.ConnectionString, invalidZip, resume: false);
            first.Quarantined.Should().Be(1);

            var second = await RunImport(database.ConnectionString, validZip, resume: false);
            second.Imported.Should().Be(5);
            second.Quarantined.Should().Be(0);

            await using var context = database.CreateContext();
            var quarantine = await context.Set<ImportQuarantine>().SingleAsync();
            quarantine.DeletedAt.Should().NotBeNull("успешный импорт снимает карантин с файла");
            (await context.Set<Vote>().CountAsync()).Should().Be(5);

            var report = await RunVerify(database.ConnectionString);
            report.Rows.Single(x => x.Name == "Всего голосований").Actual.Should().Be(5, "снятый карантин не учитывается дважды");
            report.QuarantineVotes.Should().Be(0);
        }
        finally
        {
            File.Delete(invalidZip);
            File.Delete(validZip);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Регресс (Б4): коллизия кодов неизвестных фракций разрешается суффиксом и не роняет пачку
    /// </summary>
    [SkippableFact]
    public async Task Import_UnknownFactionCodeCollision_CreatesDistinctFactions()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var entries = new[] { "archive_126282", "archive_18642" }.Select(name =>
        {
            var (entryName, json) = FixtureEntry(name);
            var node = JsonNode.Parse(json)!;
            var title = name == "archive_126282"
                ? "Тестовая фракция с очень длинным названием номер один"
                : "Тестовая фракция с очень длинным названием номер два";
            foreach (var deputyVote in node["votes"]!.AsArray())
            {
                deputyVote!["deputy"]!["faction"]!["id"] = "99900001";
                deputyVote!["deputy"]!["faction"]!["title"] = title;
            }

            return (entryName, node.ToJsonString());
        }).ToArray();
        var zipPath = CreateZip(entries);
        try
        {
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false);
            summary.Imported.Should().Be(2, "коллизия кодов неизвестных фракций не роняет пачку");
            summary.Quarantined.Should().Be(0);

            await using var context = database.CreateContext();
            var codes = await context.Set<Faction>().AsNoTracking()
                .Where(x => x.Code.StartsWith("F99900001"))
                .Select(x => x.Code)
                .ToListAsync();
            codes.Should().BeEquivalentTo(["F99900001", "F99900001_2"]);
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Регресс (Б4): порядок файлов в архиве не хронологический — более старое голосование
    /// не перезаписывает имя депутата, уточнённое более свежим
    /// </summary>
    [SkippableFact]
    public async Task Import_OlderVoteLast_KeepsNewerDeputyName()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var (_, baseJson) = FixtureEntry("archive_126282");
        var newerNode = JsonNode.Parse(baseJson)!;
        newerNode["id"] = "999995";
        newerNode["datetime"] = "2024-03-14T15:01:13+0300";
        newerNode["votes"]![0]!["deputy"]!["name"] = "Абакаров Хизри Магомедович (новое имя)";
        var olderNode = JsonNode.Parse(baseJson)!;
        olderNode["id"] = "999994";
        var entries = new (string EntryName, string Json)[]
        {
            ("vote-master/poll/2024-03-14/999995.json", newerNode.ToJsonString()),
            ("vote-master/poll/2024-03-13/999994.json", olderNode.ToJsonString()),
        };
        var zipPath = CreateZip(entries);
        try
        {
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false);
            summary.Imported.Should().Be(2);

            await using var context = database.CreateContext();
            var deputy = await context.Set<Deputy>().AsNoTracking().SingleAsync(x => x.SiteId == 99113478);
            deputy.FullName.Should().Be("Абакаров Хизри Магомедович (новое имя)", "более старое голосование не перезаписывает имя");
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Bulk-режим: после импорта схема идентична схеме миграций (индексы и FK восстановлены из EF-модели)
    /// </summary>
    [SkippableFact]
    public async Task Import_BulkMode_RestoresSchemaIdenticalToMigrations()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        try
        {
            var before = await SnapshotStreamSchema(database);
            var summary = await RunImport(database.ConnectionString, zipPath, resume: false, bulk: true);
            summary.DeputyVotesImported.Should().Be(1340);
            summary.FactionStatsImported.Should().Be(20);
            var after = await SnapshotStreamSchema(database);
            after.Should().BeEquivalentTo(before, "bulk-режим обязан восстановить индексы и FK в точности как в миграциях");
        }
        finally
        {
            File.Delete(zipPath);
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Bulk-режим под ограниченной ролью (не суперпользователь, не владелец таблиц): мягкий переход в обычный режим
    /// </summary>
    [SkippableFact]
    public async Task Import_BulkMode_RestrictedRole_FallsBackToNormalMode()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        var zipPath = CreateMiniZip();
        var roleName = "dl_bulk_test_" + Guid.NewGuid().ToString("N")[..12];
        var password = Guid.NewGuid().ToString("N");
        var roleCreated = false;
        try
        {
            var databaseName = new NpgsqlConnectionStringBuilder(database.ConnectionString).Database;
            await using (var adminContext = database.CreateContext())
            {
                await adminContext.Database.ExecuteSqlRawAsync(
                    "CREATE ROLE \"" + roleName + "\" LOGIN PASSWORD '" + password + "' NOSUPERUSER NOCREATEDB NOCREATEROLE");
                roleCreated = true;
                await adminContext.Database.ExecuteSqlRawAsync("GRANT CONNECT ON DATABASE \"" + databaseName + "\" TO \"" + roleName + "\"");
                await adminContext.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO \"" + roleName + "\"");
                await adminContext.Database.ExecuteSqlRawAsync("GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO \"" + roleName + "\"");
            }

            var restrictedConnection = new NpgsqlConnectionStringBuilder(database.ConnectionString)
            {
                Username = roleName,
                Password = password,
            }.ConnectionString;
            var summary = await RunImport(restrictedConnection, zipPath, resume: false, bulk: true);
            summary.DeputyVotesImported.Should().Be(1340, "без прав на DDL импорт обязан завершиться в обычном режиме");

            await using var checkContext = database.CreateContext();
            var disabledTriggers = await checkContext.Database
                .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_trigger " +
                    "WHERE tgrelid IN ('\"DeputyVotes\"'::regclass, '\"VoteFactionStats\"'::regclass) AND tgenabled = 'D'")
                .SingleAsync();
            disabledTriggers.Should().Be(0, "не-суперпользователь не отключает триггеры");
        }
        finally
        {
            File.Delete(zipPath);
            if (roleCreated)
            {
                await using var cleanupContext = database.CreateContext();
                await cleanupContext.Database.ExecuteSqlRawAsync("DROP OWNED BY \"" + roleName + "\"");
                await cleanupContext.Database.ExecuteSqlRawAsync("DROP ROLE \"" + roleName + "\"");
            }

            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Снимок схемы потоковых таблиц: определения индексов и FK для сравнения до/после bulk-импорта
    /// </summary>
    private static async Task<List<string>> SnapshotStreamSchema(DatabaseFixture database)
    {
        await using var context = database.CreateContext();
        var indexes = await context.Database
            .SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE tablename IN ('DeputyVotes', 'VoteFactionStats')")
            .ToListAsync();
        var foreignKeys = await context.Database
            .SqlQueryRaw<string>("SELECT conname || '|' || pg_get_constraintdef(oid) AS \"Value\" FROM pg_constraint " +
                "WHERE conrelid IN ('\"DeputyVotes\"'::regclass, '\"VoteFactionStats\"'::regclass) AND contype = 'f'")
            .ToListAsync();
        return indexes.Concat(foreignKeys).OrderBy(x => x, StringComparer.Ordinal).ToList();
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

    private static async Task<ImportSummary> RunImport(string connectionString, string zipPath, bool resume, bool bulk = false)
    {
        await using var provider = ImporterHost.BuildServiceProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<ArchiveImporter>();
        var options = new ImportOptions { ZipPath = zipPath, BatchSize = 2, Resume = resume, Bulk = bulk };
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
        var votes = context.Set<Vote>();
        var deputyVotes = context.Set<DeputyVote>();
        var statsRows = await context.Set<VoteFactionStats>().AsNoTracking().ToListAsync();
        return new DbCounts(
            await votes.CountAsync(),
            await deputyVotes.CountAsync(),
            await context.Set<Deputy>().CountAsync(),
            await context.Set<FactionPeriod>().CountAsync(),
            await context.Set<DeputyFactionMembership>().CountAsync(),
            await context.Set<Bill>().CountAsync(),
            await context.Set<ImportQuarantine>().CountAsync(),
            await votes.SumAsync(x => x.AcceptCountTotal),
            await votes.SumAsync(x => x.DeclineCountTotal),
            await votes.SumAsync(x => x.AbstainCountTotal),
            await votes.SumAsync(x => x.NoneCountTotal),
            await deputyVotes.CountAsync(x => x.Result == VoteResult.Accept),
            await deputyVotes.CountAsync(x => x.Result == VoteResult.Decline),
            await deputyVotes.CountAsync(x => x.Result == VoteResult.Abstain),
            await deputyVotes.CountAsync(x => x.Result == VoteResult.None),
            string.Join("|", await context.Set<Deputy>().OrderBy(x => x.SiteId).Select(x => x.FullName).ToListAsync()),
            statsRows.Count,
            StatsFingerprint(statsRows));
    }

    /// <summary>
    /// Отпечаток фракционных агрегатов без привязки к идентификаторам: реимпорт сохраняет
    /// все значения, но строки могут получить другие ключи периодов
    /// </summary>
    private static string StatsFingerprint(List<VoteFactionStats> rows)
    {
        return string.Join("|", rows
            .OrderBy(x => x.AcceptCount).ThenBy(x => x.DeclineCount).ThenBy(x => x.AbstainCount).ThenBy(x => x.NoneCount)
            .ThenBy(x => x.MemberCount).ThenBy(x => x.VotedCount)
            .ThenBy(x => x.AcceptPctMembers).ThenBy(x => x.DeclinePctMembers).ThenBy(x => x.AbstainPctMembers).ThenBy(x => x.NonePctMembers)
            .ThenBy(x => x.AcceptPctVoted).ThenBy(x => x.DeclinePctVoted).ThenBy(x => x.AbstainPctVoted)
            .Select(x => FormattableString.Invariant($"{x.AcceptCount},{x.DeclineCount},{x.AbstainCount},{x.NoneCount},{x.MemberCount},{x.VotedCount},{x.AcceptPctMembers},{x.DeclinePctMembers},{x.AbstainPctMembers},{x.NonePctMembers},{x.AcceptPctVoted},{x.DeclinePctVoted},{x.AbstainPctVoted}")));
    }

    private sealed record DbCounts(
        long Votes, long DeputyVotes, long Deputies, long Periods, long Memberships, long Bills, long Quarantine,
        long AcceptTotal, long DeclineTotal, long AbstainTotal, long NoneTotal,
        long DeputyAccept, long DeputyDecline, long DeputyAbstain, long DeputyNone,
        string DeputyNames, long StatsRows, string StatsFingerprint);
}