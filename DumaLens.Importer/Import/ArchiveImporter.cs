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
using System.IO.Compression;
using System.Text;

namespace DumaLens.Importer.Import;

/// <summary>
/// Потоковый импорт архива vote-master.zip: разбор и валидация файлов, upsert голосований,
/// построение справочников по ходу, массовая вставка индивидуальных голосов и фракционных
/// агрегатов через COPY, карантин аномалий; пачка обрабатывается в одной транзакции
/// </summary>
public sealed class ArchiveImporter
{
    private const string VoteEntryPrefix = "vote-master/poll/";
    private const string CopyCommand = "COPY \"DeputyVotes\" (\"VoteId\", \"DeputyId\", \"FactionPeriodId\", \"Result\") FROM STDIN (FORMAT BINARY)";
    private const string CopyStatsCommand = "COPY \"VoteFactionStats\" (\"VoteId\", \"FactionPeriodId\", \"FactionId\", \"AcceptCount\", \"DeclineCount\", \"AbstainCount\", \"NoneCount\", \"MemberCount\", \"VotedCount\", \"AcceptPctMembers\", \"DeclinePctMembers\", \"AbstainPctMembers\", \"NonePctMembers\", \"AcceptPctVoted\", \"DeclinePctVoted\", \"AbstainPctVoted\") FROM STDIN (FORMAT BINARY)";

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
        ImportLog log)
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
    }

    /// <summary>
    /// Импортировать архив; при сбое пачки транзакция откатывается, повторный запуск
    /// с <see cref="ImportOptions.Resume"/> продолжает с необработанных голосований
    /// </summary>
    public async Task<ImportSummary> Import(ImportOptions options, CancellationToken cancellationToken)
    {
        var summary = new ImportSummary();
        var startedAt = dateTimeProvider.UtcNow();

        await context.Database.MigrateAsync(cancellationToken);
        await catalog.Load(cancellationToken);

        using var archive = ZipFile.OpenRead(options.ZipPath);
        var entries = archive.Entries.Where(IsVoteEntry).ToList();
        summary.TotalFiles = entries.Count;
        log.Info($"Архив {options.ZipPath}: файлов голосований {entries.Count:N0}, пачка {options.BatchSize}, режим {(options.Resume ? "resume" : "полный")}");

        var batch = new List<BatchItem>(options.BatchSize);
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            batch.Add(await ReadBatchItem(entry, cancellationToken));
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

        summary.Elapsed = dateTimeProvider.UtcNow() - startedAt;
        log.Info($"Импорт завершён за {summary.Elapsed:hh\\:mm\\:ss}: импортировано {summary.Imported:N0}, пропущено {summary.Skipped:N0}, карантин {summary.Quarantined:N0}, индивидуальных голосов {summary.DeputyVotesImported:N0}, строк фракционной статистики {summary.FactionStatsImported:N0}");
        return summary;
    }

    private static bool IsVoteEntry(ZipArchiveEntry entry)
    {
        return entry.FullName.StartsWith(VoteEntryPrefix, StringComparison.OrdinalIgnoreCase)
            && entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BatchItem> ReadBatchItem(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        string rawJson;
        using (var stream = entry.Open())
        using (var textReader = new StreamReader(stream, Encoding.UTF8))
        {
            rawJson = await textReader.ReadToEndAsync(cancellationToken);
        }

        var parsed = parser.Parse(rawJson);
        var reasons = new List<string>();
        foreach (var issue in parsed.Issues)
        {
            reasons.Add($"[{issue.Code}] {issue.Message}");
        }

        var fileName = Path.GetFileNameWithoutExtension(entry.FullName);
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

        if (reasons.Count > 0)
        {
            return BatchItem.Quarantined(entry.FullName, rawJson, parsed, reasons);
        }

        return BatchItem.Valid(entry.FullName, rawJson, parsed, normalized!, convocation!);
    }

    private async Task FlushBatch(List<BatchItem> batch, bool resume, ImportSummary summary, CancellationToken cancellationToken)
    {
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

        var deputyVotes = new List<DeputyVote>();
        var reprocessedVoteIds = new List<Guid>();
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
            // Итог и процент — факты сайта (ADR-9): реимпорт архива их не затирает
            if (isNew || vote.Source == VoteSource.Archive)
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
                var deputyId = catalog.EnsureDeputy(deputyVote.DeputySiteId, deputyVote.FullName, date);
                Guid? periodId = null;
                if (deputyVote.FactionSiteId.HasValue && deputyVote.FactionTitle != null)
                {
                    periodId = catalog.EnsureFactionPeriod(deputyVote.FactionSiteId.Value, deputyVote.FactionTitle, convocation, date);
                    catalog.TrackMembership(deputyId, periodId.Value, date);
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

                deputyVotes.Add(new DeputyVote
                {
                    VoteId = vote.Id,
                    DeputyId = deputyId,
                    FactionPeriodId = periodId,
                    Result = deputyVote.Result!.Value,
                });
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

        var statsRows = BuildFactionStats(deputyVotes);

        catalog.FlushUpdates();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (reprocessedVoteIds.Count > 0)
        {
            await context.Set<DeputyVote>().Where(x => reprocessedVoteIds.Contains(x.VoteId)).ExecuteDeleteAsync(cancellationToken);
            await context.Set<VoteFactionStats>().Where(x => reprocessedVoteIds.Contains(x.VoteId)).ExecuteDeleteAsync(cancellationToken);
        }

        if (deputyVotes.Count > 0)
        {
            await CopyDeputyVotes(deputyVotes, cancellationToken);
        }

        if (statsRows.Count > 0)
        {
            await CopyFactionStats(statsRows, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        summary.DeputyVotesImported += deputyVotes.Count;
        summary.FactionStatsImported += statsRows.Count;
        summary.Batches++;
    }

    /// <summary>
    /// Строит фракционные агрегаты пачки из индивидуальных голосов (формулы — в сервисе, Э4)
    /// </summary>
    private List<VoteFactionStats> BuildFactionStats(List<DeputyVote> deputyVotes)
    {
        if (deputyVotes.Count == 0)
        {
            return [];
        }

        var factionIdsByPeriod = catalog.GetFactionIdsByPeriodId();
        var statsRows = new List<VoteFactionStats>();
        foreach (var voteGroup in deputyVotes.GroupBy(x => x.VoteId))
        {
            statsRows.AddRange(statsService.BuildStats(voteGroup.Key, voteGroup.ToList(), factionIdsByPeriod));
        }

        return statsRows;
    }

    private async Task CopyDeputyVotes(List<DeputyVote> rows, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await using var importer = await connection.BeginBinaryImportAsync(CopyCommand, cancellationToken);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(row.VoteId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.DeputyId, NpgsqlDbType.Uuid, cancellationToken);
            if (row.FactionPeriodId.HasValue)
            {
                await importer.WriteAsync(row.FactionPeriodId.Value, NpgsqlDbType.Uuid, cancellationToken);
            }
            else
            {
                await importer.WriteNullAsync(cancellationToken);
            }

            await importer.WriteAsync((short)row.Result, NpgsqlDbType.Smallint, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
    }

    private async Task CopyFactionStats(List<VoteFactionStats> rows, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
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