using DumaLens.Common.Contracts;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Models;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.WriteRepositories;
using DumaLens.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Channels;

namespace DumaLens.Importer.Import;

/// <summary>
/// Потоковый импорт архива vote-master.zip: разбор и валидация файлов, upsert голосований,
/// построение справочников по ходу, массовая вставка индивидуальных голосов и фракционных
/// агрегатов через COPY, карантин аномалий; пачка обрабатывается в одной транзакции
/// </summary>
/// <remarks>
/// Конвейер «чтение ZIP → параллельный разбор/валидация → запись пачки»: чтение архива
/// последовательное (ZipArchive не потокобезопасен), разбор JSON и валидация выполняются
/// несколькими рабочими задачами, запись — единственным потребителем. Порядок элементов
/// не влияет на результат: справочники обновляются по датам голосований, а не по порядку файлов.
/// Ускорение записи — <see cref="BulkLoadSchema"/>: отключение FK-триггеров, bulk-режим
/// для пустых таблиц; строки перед COPY сортируются по ключам для последовательных вставок в индексы.
/// </remarks>
public sealed class ArchiveImporter
{
    private const string VoteEntryPrefix = "vote-master/poll/";

    private readonly DumaLensContext context;
    private readonly IReader reader;
    private readonly IUnitOfWork unitOfWork;
    private readonly IVoteWriteRepository voteWriter;
    private readonly IImportQuarantineWriteRepository quarantineWriter;
    private readonly ReferenceCatalog catalog;
    private readonly IVoteArchiveParser parser;
    private readonly ITitleNormalizer normalizer;
    private readonly IVoteParseValidator validator;
    private readonly IVoteFactionStatsService statsService;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly ImportLog log;
    private readonly BulkLoadSchema schema;

    private long readTicks;
    private long parseTicks;
    private long validateTicks;
    private long queryTicks;
    private long buildTicks;
    private long statsTicks;
    private long saveTicks;
    private long deleteTicks;
    private long copyVotesTicks;
    private long copyStatsTicks;
    private long commitTicks;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ArchiveImporter(
        DumaLensContext context,
        IReader reader,
        IUnitOfWork unitOfWork,
        IVoteWriteRepository voteWriter,
        IImportQuarantineWriteRepository quarantineWriter,
        ReferenceCatalog catalog,
        IVoteArchiveParser parser,
        ITitleNormalizer normalizer,
        IVoteParseValidator validator,
        IVoteFactionStatsService statsService,
        IDateTimeProvider dateTimeProvider,
        ImportLog log,
        BulkLoadSchema schema)
    {
        this.context = context;
        this.reader = reader;
        this.unitOfWork = unitOfWork;
        this.voteWriter = voteWriter;
        this.quarantineWriter = quarantineWriter;
        this.catalog = catalog;
        this.parser = parser;
        this.normalizer = normalizer;
        this.validator = validator;
        this.statsService = statsService;
        this.dateTimeProvider = dateTimeProvider;
        this.log = log;
        this.schema = schema;
    }

    /// <summary>
    /// Импортировать архив; при сбое пачки транзакция откатывается, повторный запуск
    /// с <see cref="ImportOptions.Resume"/> продолжает с необработанных голосований
    /// </summary>
    public async Task<ImportSummary> Import(ImportOptions options, CancellationToken cancellationToken)
    {
        var summary = new ImportSummary();
        var startedAt = dateTimeProvider.UtcNow();

        // Миграции могут создавать индексы на десятках миллионов строк — дефолтные 30 с Npgsql не годятся
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));
        await context.Database.MigrateAsync(cancellationToken);
        await catalog.Load(cancellationToken);
        await schema.PrepareAsync(options.Bulk, cancellationToken);

        try
        {
            using var archive = ZipFile.OpenRead(options.ZipPath);
            var entries = archive.Entries.Where(IsVoteEntry);
            if (options.MaxFiles > 0)
            {
                entries = entries.Take(options.MaxFiles);
            }

            var entryList = entries.ToList();
            summary.TotalFiles = entryList.Count;
            var workerCount = options.Workers > 0 ? options.Workers : Math.Max(1, Environment.ProcessorCount - 2);
            log.Info($"Архив {options.ZipPath}: файлов голосований {entryList.Count:N0}, пачка {options.BatchSize}, задач разбора {workerCount}, режим {(options.Resume ? "resume" : "полный")}{(options.Bulk ? ", bulk" : string.Empty)}");

            await RunPipeline(entryList, options, summary, startedAt, workerCount, cancellationToken);
        }
        finally
        {
            await schema.RestoreAsync(CancellationToken.None);
        }

        summary.Elapsed = dateTimeProvider.UtcNow() - startedAt;
        log.Info($"Импорт завершён за {summary.Elapsed:hh\\:mm\\:ss}: импортировано {summary.Imported:N0}, пропущено {summary.Skipped:N0}, карантин {summary.Quarantined:N0}, индивидуальных голосов {summary.DeputyVotesImported:N0}, строк фракционной статистики {summary.FactionStatsImported:N0}");
        LogTimings();
        return summary;
    }

    private void LogTimings()
    {
        log.Info($"Профиль фаз, с: чтение ZIP {Seconds(readTicks):F1}, разбор JSON {Seconds(parseTicks):F1}, валидация {Seconds(validateTicks):F1}, запросы пачки {Seconds(queryTicks):F1}, сборка сущностей {Seconds(buildTicks):F1}, агрегаты {Seconds(statsTicks):F1}, SaveChanges {Seconds(saveTicks):F1}, удаление старых версий {Seconds(deleteTicks):F1}, COPY голосов {Seconds(copyVotesTicks):F1}, COPY статистики {Seconds(copyStatsTicks):F1}, коммит {Seconds(commitTicks):F1}");
    }

    private static double Seconds(long ticks)
    {
        return (double)ticks / Stopwatch.Frequency;
    }

    private static long ElapsedSince(long startTimestamp)
    {
        return Stopwatch.GetTimestamp() - startTimestamp;
    }

    private static bool IsVoteEntry(ZipArchiveEntry entry)
    {
        return entry.FullName.StartsWith(VoteEntryPrefix, StringComparison.OrdinalIgnoreCase)
            && entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Конвейер импорта: последовательное чтение ZIP, параллельный разбор/валидация,
    /// последовательная запись пачек единственным потребителем
    /// </summary>
    /// <remarks>
    /// Каналы ограничены (двойной размер пачки) — обратное давление не даёт разбору убежать
    /// вперёд записи и ограничивает память. Ошибка любой стадии завершает конвейер:
    /// потребитель отменяет связанный токен, производители наблюдаются и первая ошибка пробрасывается.
    /// </remarks>
    private async Task RunPipeline(
        List<ZipArchiveEntry> entries,
        ImportOptions options,
        ImportSummary summary,
        DateTimeOffset startedAt,
        int workerCount,
        CancellationToken cancellationToken)
    {
        var rawChannel = Channel.CreateBounded<RawEntry>(new BoundedChannelOptions(options.BatchSize * 2)
        {
            SingleWriter = true,
            SingleReader = false,
        });
        var itemChannel = Channel.CreateBounded<BatchItem>(new BoundedChannelOptions(options.BatchSize * 2)
        {
            SingleWriter = false,
            SingleReader = true,
        });
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = linkedCts.Token;
        var remainingWorkers = workerCount;

        var readerTask = Task.Run(async () =>
        {
            try
            {
                foreach (var entry in entries)
                {
                    token.ThrowIfCancellationRequested();
                    var start = Stopwatch.GetTimestamp();
                    string rawJson;
                    using (var stream = entry.Open())
                    using (var textReader = new StreamReader(stream, Encoding.UTF8))
                    {
                        rawJson = await textReader.ReadToEndAsync(token);
                    }

                    Interlocked.Add(ref readTicks, ElapsedSince(start));
                    await rawChannel.Writer.WriteAsync(new RawEntry(entry.FullName, rawJson), token);
                }

                rawChannel.Writer.TryComplete();
            }
            catch (Exception exception)
            {
                rawChannel.Writer.TryComplete(exception);
                throw;
            }
        }, token);

        var workerTasks = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
        {
            workerTasks[i] = Task.Run(async () =>
            {
                try
                {
                    await foreach (var raw in rawChannel.Reader.ReadAllAsync(token))
                    {
                        var item = ParseBatchItem(raw.EntryName, raw.RawJson);
                        await itemChannel.Writer.WriteAsync(item, token);
                    }
                }
                catch (Exception exception)
                {
                    itemChannel.Writer.TryComplete(exception);
                    throw;
                }
                finally
                {
                    if (Interlocked.Decrement(ref remainingWorkers) == 0)
                    {
                        itemChannel.Writer.TryComplete();
                    }
                }
            }, token);
        }

        Exception? error = null;
        var batch = new List<BatchItem>(options.BatchSize);
        try
        {
            await foreach (var item in itemChannel.Reader.ReadAllAsync(token))
            {
                batch.Add(item);
                if (batch.Count >= options.BatchSize)
                {
                    await FlushBatch(batch, options.Resume, summary, cancellationToken);
                    batch.Clear();
                    LogProgress(summary, startedAt);
                }
            }

            if (batch.Count > 0)
            {
                await FlushBatch(batch, options.Resume, summary, cancellationToken);
                LogProgress(summary, startedAt);
            }
        }
        catch (Exception exception)
        {
            error = exception;
            linkedCts.Cancel();
        }

        try
        {
            await Task.WhenAll(workerTasks.Append(readerTask));
        }
        catch (Exception exception)
        {
            error ??= exception;
        }

        if (error != null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    /// <summary>
    /// Разбор и валидация одного файла; выполняется рабочими задачами конвейера параллельно
    /// </summary>
    /// <remarks>
    /// Потокобезопасно: парсер, нормализатор и валидатор не имеют состояния,
    /// <see cref="ReferenceCatalog.FindConvocation"/> только читает загруженные созывы.
    /// </remarks>
    private BatchItem ParseBatchItem(string entryName, string rawJson)
    {
        var start = Stopwatch.GetTimestamp();
        var parsed = parser.Parse(rawJson);
        Interlocked.Add(ref parseTicks, ElapsedSince(start));
        start = Stopwatch.GetTimestamp();
        var reasons = new List<string>();
        foreach (var issue in parsed.Issues)
        {
            reasons.Add($"[{issue.Code}] {issue.Message}");
        }

        var fileName = Path.GetFileNameWithoutExtension(entryName);
        if (!long.TryParse(fileName, out var fileSiteId) || fileSiteId != parsed.SiteId)
        {
            reasons.Add($"Идентификатор в имени файла «{fileName}» не совпадает с id в JSON ({parsed.SiteId})");
        }

        var seenDeputies = new HashSet<long>();
        var duplicateDeputies = new List<long>();
        foreach (var deputyVote in parsed.DeputyVotes)
        {
            if (!seenDeputies.Add(deputyVote.DeputySiteId))
            {
                duplicateDeputies.Add(deputyVote.DeputySiteId);
            }
        }

        if (duplicateDeputies.Count > 0)
        {
            reasons.Add($"Повторяющиеся депутаты в голосовании: {string.Join(", ", duplicateDeputies.Distinct().Take(10))}");
        }

        NormalizedTitle? normalized = null;
        ConvocationDbModel? convocation = null;
        if (reasons.Count == 0)
        {
            normalized = normalizer.Normalize(parsed.Title);
            var date = DateOnly.FromDateTime(parsed.VotedAt.DateTime);
            convocation = catalog.FindConvocation(date);
            if (convocation == null)
            {
                reasons.Add($"Дата {date:yyyy-MM-dd} не входит ни в один созыв");
            }
            else
            {
                foreach (var issue in validator.Validate(parsed, convocation.DateFrom, convocation.DateTo))
                {
                    reasons.Add($"[{issue.Code}] {issue.Message}");
                }
            }
        }

        Interlocked.Add(ref validateTicks, ElapsedSince(start));
        return reasons.Count > 0
            ? BatchItem.Quarantined(entryName, rawJson, parsed, reasons)
            : BatchItem.Valid(entryName, rawJson, parsed, normalized!, convocation!);
    }

    private sealed record RawEntry(string EntryName, string RawJson);

    private async Task FlushBatch(List<BatchItem> batch, bool resume, ImportSummary summary, CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        var validItems = batch.Where(x => x.IsValid).ToList();
        var siteIds = validItems.Select(x => x.Parsed.SiteId).Distinct().ToList();
        var existingVotes = siteIds.Count == 0
            ? []
            : await reader.Read<Vote>().NotDeletedAt().Where(x => siteIds.Contains(x.SiteId)).ToListAsync(cancellationToken);
        var existingBySiteId = existingVotes.ToDictionary(x => x.SiteId);

        var invalidItems = batch.Where(x => !x.IsValid).ToList();
        var invalidNames = invalidItems.Select(x => x.EntryName).ToList();
        var existingQuarantineFiles = invalidNames.Count == 0
            ? []
            : await reader.Read<ImportQuarantine>()
                .NotDeletedAt()
                .Where(x => x.SourceFile != null && invalidNames.Contains(x.SourceFile))
                .Select(x => x.SourceFile!)
                .ToListAsync(cancellationToken);
        var quarantinedFiles = existingQuarantineFiles.ToHashSet(StringComparer.Ordinal);

        // Ранее забракованные файлы, ставшие годными: карантин снимается, чтобы сверка не считала их дважды
        var validNames = validItems.Select(x => x.EntryName).ToList();
        var staleQuarantine = validNames.Count == 0
            ? []
            : await reader.Read<ImportQuarantine>()
                .NotDeletedAt()
                .Where(x => x.SourceFile != null && validNames.Contains(x.SourceFile))
                .ToListAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        queryTicks += ElapsedSince(start);
        start = Stopwatch.GetTimestamp();

        var pending = new List<DeputyVoteLink>();
        var reprocessedVoteIds = new List<Guid>();
        var reprocessedVoteRowIds = new List<int>();
        foreach (var item in validItems)
        {
            var parsed = item.Parsed;
            if (resume && existingBySiteId.ContainsKey(parsed.SiteId))
            {
                summary.Skipped++;
                continue;
            }

            var normalized = item.Normalized!;
            var convocation = item.Convocation!;
            var date = DateOnly.FromDateTime(parsed.VotedAt.DateTime);
            var billId = catalog.EnsureBill(normalized.BillNumber);

            existingBySiteId.TryGetValue(parsed.SiteId, out var vote);
            var isNew = false;
            if (vote is null)
            {
                isNew = true;
                vote = new Vote
                {
                    Id = Guid.NewGuid(),
                };
            }

            vote.SiteId = parsed.SiteId;
            vote.Title = parsed.Title;
            vote.VotedAt = parsed.VotedAt;
            vote.UtcOffsetMinutes = parsed.UtcOffsetMinutes;
            vote.ConvocationId = convocation.Id;
            vote.BillId = billId;
            vote.Stage = normalized.Stage;
            vote.StageRaw = normalized.StageRaw;
            vote.HasResults = parsed.HasResults;
            // Итог и процент — факты сайта (ADR-9): реимпорт архива их не затирает,
            // включая Outcome/процент, добранные кроулером у архивных голосований (Э7)
            if (isNew)
            {
                vote.Outcome = VoteOutcome.Unknown;
                vote.AcceptPercentSite = null;
                vote.Source = VoteSource.Archive;
            }

            var acceptCount = 0;
            var declineCount = 0;
            var abstainCount = 0;
            var noneCount = 0;
            foreach (var deputyVote in parsed.DeputyVotes)
            {
                var deputy = catalog.EnsureDeputy(deputyVote.DeputySiteId, deputyVote.FullName, date);
                FactionPeriod? period = null;
                if (deputyVote.FactionSiteId.HasValue && deputyVote.FactionTitle != null)
                {
                    period = catalog.EnsureFactionPeriod(deputyVote.FactionSiteId.Value, deputyVote.FactionTitle, convocation, date);
                    catalog.TrackMembership(deputy.Id, period.Id, date);
                }

                switch (deputyVote.Result)
                {
                    case VoteResult.Accept:
                        acceptCount++;
                        break;
                    case VoteResult.Decline:
                        declineCount++;
                        break;
                    case VoteResult.Abstain:
                        abstainCount++;
                        break;
                    case VoteResult.None:
                        noneCount++;
                        break;
                }

                pending.Add(new DeputyVoteLink(vote, deputy, period, deputyVote.Result!.Value));
            }

            vote.AcceptCountTotal = acceptCount;
            vote.DeclineCountTotal = declineCount;
            vote.AbstainCountTotal = abstainCount;
            vote.NoneCountTotal = noneCount;

            if (isNew)
            {
                voteWriter.Add(vote);
            }
            else
            {
                voteWriter.Edit(vote);
                reprocessedVoteIds.Add(vote.Id);
                reprocessedVoteRowIds.Add(vote.RowId);
            }

            summary.Imported++;
        }

        foreach (var item in invalidItems)
        {
            if (!quarantinedFiles.Add(item.EntryName))
            {
                summary.Skipped++;
                continue;
            }

            quarantineWriter.Add(new ImportQuarantine
            {
                Id = Guid.NewGuid(),
                SourceFile = item.EntryName,
                SiteId = item.Parsed.SiteId > 0 ? item.Parsed.SiteId : null,
                Reason = TruncateReason(string.Join("; ", item.QuarantineReasons)),
                RawJson = item.RawJson,
            });
            summary.Quarantined++;
        }

        foreach (var record in staleQuarantine)
        {
            quarantineWriter.Delete(record);
        }

        if (staleQuarantine.Count > 0)
        {
            log.Info($"Снят карантин с {staleQuarantine.Count} файлов, успешно импортированных в этом проходе");
        }

        buildTicks += ElapsedSince(start);
        start = Stopwatch.GetTimestamp();

        // SaveChanges до материализации голосов: RowId новых голосований/депутатов/периодов присваивает база
        catalog.FlushUpdates();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        saveTicks += ElapsedSince(start);
        start = Stopwatch.GetTimestamp();

        var deputyVotes = BulkCopyWriter.MaterializeDeputyVotes(pending);
        var statsRows = BulkCopyWriter.BuildFactionStats(deputyVotes, pending, catalog.GetPeriodStatsKeys(), statsService);
        statsTicks += ElapsedSince(start);

        BulkCopyWriter.SortForCopy(deputyVotes, statsRows);

        start = Stopwatch.GetTimestamp();

        if (reprocessedVoteIds.Count > 0)
        {
            await context.Set<DeputyVote>().Where(x => reprocessedVoteRowIds.Contains(x.VoteId)).ExecuteDeleteAsync(cancellationToken);
            await context.Set<VoteFactionStats>().Where(x => reprocessedVoteIds.Contains(x.VoteId)).ExecuteDeleteAsync(cancellationToken);
        }

        deleteTicks += ElapsedSince(start);
        start = Stopwatch.GetTimestamp();

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (deputyVotes.Count > 0)
        {
            await BulkCopyWriter.CopyDeputyVotes(connection, deputyVotes, cancellationToken);
        }

        copyVotesTicks += ElapsedSince(start);
        start = Stopwatch.GetTimestamp();

        if (statsRows.Count > 0)
        {
            await BulkCopyWriter.CopyFactionStats(connection, statsRows, cancellationToken);
        }

        copyStatsTicks += ElapsedSince(start);
        start = Stopwatch.GetTimestamp();

        await transaction.CommitAsync(cancellationToken);
        commitTicks += ElapsedSince(start);
        summary.DeputyVotesImported += deputyVotes.Count;
        summary.FactionStatsImported += statsRows.Count;
        summary.Batches++;
    }

    private void LogProgress(ImportSummary summary, DateTimeOffset startedAt)
    {
        var elapsed = dateTimeProvider.UtcNow() - startedAt;
        var processed = summary.Imported + summary.Skipped + summary.Quarantined;
        var percent = summary.TotalFiles == 0 ? 100.0 : processed * 100.0 / summary.TotalFiles;
        var speed = elapsed.TotalSeconds > 0 ? processed / elapsed.TotalSeconds : 0;
        log.Info($"Обработано {processed:N0}/{summary.TotalFiles:N0} ({percent:F1}%): импортировано {summary.Imported:N0}, пропущено {summary.Skipped:N0}, карантин {summary.Quarantined:N0}; индивидуальных голосов {summary.DeputyVotesImported:N0}; скорость {speed:F0} голосований/с");
    }

    private static string TruncateReason(string reason)
    {
        return reason.Length <= ImportQuarantineValidationConstants.ReasonMaxLength
            ? reason
            : reason[..ImportQuarantineValidationConstants.ReasonMaxLength];
    }
}