using DumaLens.Common.Contracts;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Importer.Import;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Models;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.WriteRepositories;
using DumaLens.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DumaLens.Importer.Crawl;

/// <summary>
/// Кроулер живого сайта vote.duma.gov.ru (Э7): обход листингов созывов, добор итога/процента
/// у архивных голосований и загрузка новых карточек после 13.03.2024 (ADR-9)
/// </summary>
/// <remarks>
/// Листинги дают исход и процент «За», которых нет в архиве; карточки — индивидуальные голоса.
/// Уже импортированные голосования не перекачиваются (пропуск по SiteId), карантин дедуплицируется
/// по SourceUrl — повторный запуск идемпотентен. Запись — пачками в одной транзакции,
/// индивидуальные голоса и фракционные агрегаты — COPY (общий BulkCopyWriter с импортом архива).
/// Пауза между запросами и повторы с экспоненциальной задержкой — вежливость к сайту.
/// </remarks>
public sealed class VoteCrawler
{
    private const int BackfillChunkSize = 1000;

    private readonly IReader reader;
    private readonly IUnitOfWork unitOfWork;
    private readonly DumaLensContext context;
    private readonly IVoteWriteRepository voteWriter;
    private readonly IImportQuarantineWriteRepository quarantineWriter;
    private readonly IVoteListingParser listingParser;
    private readonly IVotePageParser pageParser;
    private readonly IVoteParseValidator validator;
    private readonly ITitleNormalizer normalizer;
    private readonly IVoteFactionStatsService statsService;
    private readonly ReferenceCatalog catalog;
    private readonly SiteFetcher fetcher;
    private readonly RobotsChecker robots;
    private readonly ImportLog log;
    private readonly IDateTimeProvider dateTimeProvider;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteCrawler(
        IReader reader,
        IUnitOfWork unitOfWork,
        DumaLensContext context,
        IVoteWriteRepository voteWriter,
        IImportQuarantineWriteRepository quarantineWriter,
        IVoteListingParser listingParser,
        IVotePageParser pageParser,
        IVoteParseValidator validator,
        ITitleNormalizer normalizer,
        IVoteFactionStatsService statsService,
        ReferenceCatalog catalog,
        SiteFetcher fetcher,
        RobotsChecker robots,
        ImportLog log,
        IDateTimeProvider dateTimeProvider)
    {
        this.reader = reader;
        this.unitOfWork = unitOfWork;
        this.context = context;
        this.voteWriter = voteWriter;
        this.quarantineWriter = quarantineWriter;
        this.listingParser = listingParser;
        this.pageParser = pageParser;
        this.validator = validator;
        this.normalizer = normalizer;
        this.statsService = statsService;
        this.catalog = catalog;
        this.fetcher = fetcher;
        this.robots = robots;
        this.log = log;
        this.dateTimeProvider = dateTimeProvider;
    }

    /// <summary>
    /// Обойти листинги созывов и загрузить новые карточки; итог — сводка обхода
    /// </summary>
    public async Task<CrawlSummary> Crawl(CrawlOptions options, CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        var startedAt = dateTimeProvider.UtcNow();
        var summary = new CrawlSummary();
        var baseUrl = options.BaseUrl.TrimEnd('/');

        await context.Database.MigrateAsync(cancellationToken);
        await catalog.Load(cancellationToken);
        await robots.LoadAsync(baseUrl, cancellationToken);
        if (!robots.IsAllowed("/"))
        {
            throw new InvalidOperationException("robots.txt запрещает обход листингов сайта");
        }

        if (!robots.IsAllowed("/vote/"))
        {
            throw new InvalidOperationException("robots.txt запрещает обход карточек голосований");
        }

        var convocations = options.Convocation == 0
            ? catalog.GetConvocations()
            : [catalog.FindConvocationByNumber(options.Convocation)
                ?? throw new InvalidOperationException($"Созыв {options.Convocation} отсутствует в справочнике")];

        var existingBySiteId = await reader.Read<Vote>()
            .NotDeletedAt()
            .Select(x => new ExistingVoteRow(x.SiteId, x.Outcome, x.AcceptPercentSite))
            .ToDictionaryAsync(x => x.SiteId, cancellationToken);

        var quarantinedUrls = (await reader.Read<ImportQuarantine>()
                .NotDeletedAt()
                .Where(x => x.SourceUrl != null)
                .Select(x => x.SourceUrl!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var enqueued = new HashSet<long>();
        foreach (var convocation in convocations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            log.Info($"Созыв {convocation.Number} ({convocation.SiteCode}): обход листингов с страницы {options.FromPage}");
            var queue = new Queue<CardRequest>();
            var backfill = new Dictionary<long, BackfillValue>();
            var itemsSeen = 0;
            int? siteTotalCount = null;
            var page = options.FromPage;
            var lastPage = int.MaxValue;
            while (page <= lastPage)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var url = $"{baseUrl}/?convocation={convocation.SiteCode}&page={page}";
                var html = await fetcher.FetchAsync(url, options.DelayMs, cancellationToken);
                if (html is null)
                {
                    throw new InvalidOperationException($"Листинг недоступен после повторов: {url}");
                }

                var listing = listingParser.Parse(html);
                foreach (var issue in listing.Issues)
                {
                    log.Info($"[листинг {convocation.Number}/{page}] [{issue.Code}] {issue.Message}");
                }

                summary.ListingPagesFetched++;
                // Пустые элементы проверяем ДО lastPage: вне диапазона сайт отдаёт пустую страницу
                // с окном пагинации вокруг запрошенного номера (LastPageNumber раздут до запрошенной страницы)
                if (listing.Items.Count == 0)
                {
                    break;
                }

                lastPage = listing.LastPageNumber;
                siteTotalCount = listing.TotalCount ?? siteTotalCount;
                foreach (var item in listing.Items)
                {
                    itemsSeen++;
                    if (existingBySiteId.TryGetValue(item.SiteId, out var existing))
                    {
                        summary.CardsSkipped++;
                        var outcome = item.Outcome != VoteOutcome.Unknown ? item.Outcome : existing.Outcome;
                        var percent = item.AcceptPercent ?? existing.AcceptPercentSite;
                        if (outcome != existing.Outcome || percent != existing.AcceptPercentSite)
                        {
                            backfill[item.SiteId] = new BackfillValue(outcome, percent);
                        }
                    }
                    else if (enqueued.Add(item.SiteId))
                    {
                        queue.Enqueue(new CardRequest(item.SiteId, item, convocation.Id));
                    }
                }

                if (page % 50 == 0)
                {
                    log.Info($"Созыв {convocation.Number}: страница {page}/{lastPage}, элементов {itemsSeen}, в очереди {queue.Count}");
                }

                page++;
            }

            summary.ListingItemsSeen += itemsSeen;
            // Сравниваем только при полном обходе с первой страницы: при --from-page > 1 неполный охват штатен
            if (options.FromPage == 1 && siteTotalCount.HasValue && siteTotalCount.Value != itemsSeen)
            {
                log.Info($"Созыв {convocation.Number}: счётчик листинга {siteTotalCount.Value} расходится с собранным {itemsSeen} (сайт мог измениться во время обхода)");
            }

            await FlushBackfill(backfill, summary, cancellationToken);
            var processedCards = await ProcessCards(queue, options, summary, existingBySiteId, quarantinedUrls, cancellationToken);
            summary.ConvocationsProcessed++;
            log.Info($"Созыв {convocation.Number} завершён: элементов листинга {itemsSeen}, добор итога {backfill.Count}, карточек обработано {processedCards}");
        }

        summary.Elapsed = dateTimeProvider.UtcNow() - startedAt;
        log.Info($"Обход завершён за {summary.Elapsed.TotalSeconds:F0} с: созывов {summary.ConvocationsProcessed}, страниц листингов {summary.ListingPagesFetched}, элементов {summary.ListingItemsSeen}; новых голосований {summary.NewVotes}, добор итога {summary.OutcomeBackfilled}, пропущено карточек {summary.CardsSkipped}, отсутствует на сайте {summary.MissingIds}, карантин {summary.Quarantined}; индивидуальных голосов {summary.DeputyVotesImported}, агрегатов {summary.FactionStatsImported}");
        return summary;
    }

    private static void ValidateOptions(CrawlOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl не задан", nameof(options));
        }

        if (options.Convocation is < 0 or > 9)
        {
            throw new ArgumentException("Convocation должен быть 0 (все созывы) или 1–9", nameof(options));
        }

        if (options.FromPage < 1)
        {
            throw new ArgumentException("FromPage должен быть не меньше 1", nameof(options));
        }

        if (options.DelayMs < 0)
        {
            throw new ArgumentException("DelayMs должен быть неотрицательным", nameof(options));
        }

        if (options.BatchSize < 1)
        {
            throw new ArgumentException("BatchSize должен быть не меньше 1", nameof(options));
        }

        if (options.MaxCards < 0)
        {
            throw new ArgumentException("MaxCards должен быть неотрицательным", nameof(options));
        }

        if (options.Parallel < 1)
        {
            throw new ArgumentException("Parallel должен быть не меньше 1", nameof(options));
        }
    }

    /// <summary>
    /// Добор итога/процента у существующих архивных голосований: пачки по 1000, один SaveChanges на пачку
    /// </summary>
    private async Task FlushBackfill(Dictionary<long, BackfillValue> backfill, CrawlSummary summary, CancellationToken cancellationToken)
    {
        if (backfill.Count == 0)
        {
            return;
        }

        var siteIds = backfill.Keys.ToList();
        var updated = 0;
        for (var offset = 0; offset < siteIds.Count; offset += BackfillChunkSize)
        {
            var chunk = siteIds.Skip(offset).Take(BackfillChunkSize).ToList();
            var votes = await reader.Read<Vote>()
                .NotDeletedAt()
                .Where(x => chunk.Contains(x.SiteId))
                .ToListAsync(cancellationToken);
            foreach (var vote in votes)
            {
                var value = backfill[vote.SiteId];
                vote.Outcome = value.Outcome;
                vote.AcceptPercentSite = value.AcceptPercentSite;
                voteWriter.Edit(vote);
                updated++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        summary.OutcomeBackfilled += updated;
        log.Info($"Добор итога/процента из листингов: обновлено голосований {updated}");
    }

    private async Task<int> ProcessCards(
        Queue<CardRequest> queue,
        CrawlOptions options,
        CrawlSummary summary,
        Dictionary<long, ExistingVoteRow> existingBySiteId,
        HashSet<string> quarantinedUrls,
        CancellationToken cancellationToken)
    {
        var processed = 0;
        var batch = new List<CardRequest>();
        while (queue.Count > 0)
        {
            batch.Clear();
            while (batch.Count < options.BatchSize && queue.Count > 0 && (options.MaxCards == 0 || processed < options.MaxCards))
            {
                batch.Add(queue.Dequeue());
                processed++;
            }

            if (batch.Count == 0)
            {
                break;
            }

            var prefetched = await PrefetchCards(batch, options, cancellationToken);
            await FlushCardBatch(prefetched, options, summary, existingBySiteId, quarantinedUrls, cancellationToken);
            log.Info($"Карточки: обработано {processed}, новых {summary.NewVotes}, отсутствует {summary.MissingIds}, карантин {summary.Quarantined}");
        }

        return processed;
    }

    /// <summary>
    /// Загрузка карточек пачки вне транзакции: последовательно или с ограниченной параллельностью
    /// (пауза DelayMs перед каждым запросом); порядок результатов совпадает с порядком очереди
    /// </summary>
    private async Task<List<CardHtml>> PrefetchCards(List<CardRequest> batch, CrawlOptions options, CancellationToken cancellationToken)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/');
        var results = new CardHtml[batch.Count];
        if (options.Parallel == 1)
        {
            for (var i = 0; i < batch.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var html = await fetcher.FetchAsync($"{baseUrl}/vote/{batch[i].SiteId}", options.DelayMs, cancellationToken);
                results[i] = new CardHtml(batch[i], html);
            }

            return results.ToList();
        }

        using var throttle = new SemaphoreSlim(options.Parallel);
        var tasks = batch.Select(async (request, index) =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                var html = await fetcher.FetchAsync($"{baseUrl}/vote/{request.SiteId}", options.DelayMs, cancellationToken);
                results[index] = new CardHtml(request, html);
            }
            finally
            {
                throttle.Release();
            }
        }).ToArray();
        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // WhenAll бросает первое исключение, не дожидаясь остальных: дожидаем sibling-таски,
            // чтобы их исключения не остались unobserved, а Release() не попал в disposed семафор
            foreach (var task in tasks)
            {
                try
                {
                    await task;
                }
                catch
                {
                    // Исключение sibling-таска наблюдено; наружу пробрасывается первое исключение WhenAll
                }
            }

            throw;
        }

        return results.ToList();
    }

    /// <summary>
    /// Пачка загруженных карточек: разбор → валидация → голоса/карантин → SaveChanges → COPY голосов и агрегатов → коммит
    /// </summary>
    private async Task FlushCardBatch(
        List<CardHtml> prefetched,
        CrawlOptions options,
        CrawlSummary summary,
        Dictionary<long, ExistingVoteRow> existingBySiteId,
        HashSet<string> quarantinedUrls,
        CancellationToken cancellationToken)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/');
        var pending = new List<DeputyVoteLink>();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var card in prefetched)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = card.Request;
            var url = $"{baseUrl}/vote/{request.SiteId}";
            var html = card.Html;
            if (html is null)
            {
                summary.MissingIds++;
                log.Info($"Голосование {request.SiteId} отсутствует на сайте (устойчивый 500 или недоступно): {url}");
                continue;
            }

            var parsed = pageParser.Parse(html, request.SiteId);
            var reasons = parsed.Issues.Select(x => $"[{x.Code}] {x.Message}").ToList();
            var duplicateDeputies = parsed.DeputyVotes
                .Where(x => x.DeputySiteId != 0)
                .GroupBy(x => x.DeputySiteId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateDeputies.Count > 0)
            {
                reasons.Add($"Повторяющиеся депутаты в карточке: {string.Join(", ", duplicateDeputies)}");
            }

            ConvocationDbModel? convocation = null;
            if (reasons.Count == 0)
            {
                convocation = parsed.ConvocationSiteCode is not null
                    ? catalog.FindConvocationBySiteCode(parsed.ConvocationSiteCode)
                    : catalog.FindConvocation(DateOnly.FromDateTime(parsed.VotedAt.DateTime));
                if (convocation is null)
                {
                    reasons.Add($"Созыв не определён (код {parsed.ConvocationSiteCode ?? "отсутствует"}, дата {parsed.VotedAt:dd.MM.yyyy})");
                }
                else if (convocation.Id != request.ExpectedConvocationId)
                {
                    reasons.Add($"Код созыва {parsed.ConvocationSiteCode} в карточке не соответствует листингу");
                }
                else
                {
                    reasons.AddRange(validator.Validate(parsed, convocation.DateFrom, convocation.DateTo)
                        .Select(x => $"[{x.Code}] {x.Message}"));
                }
            }

            if (reasons.Count > 0)
            {
                if (quarantinedUrls.Add(url))
                {
                    quarantineWriter.Add(new ImportQuarantine
                    {
                        Id = Guid.NewGuid(),
                        SourceUrl = url,
                        SiteId = request.SiteId,
                        Reason = TruncateReason(string.Join("; ", reasons)),
                        RawJson = html,
                    });
                    summary.Quarantined++;
                }

                continue;
            }

            var normalized = normalizer.Normalize(parsed.Title);
            var date = DateOnly.FromDateTime(parsed.VotedAt.DateTime);
            var billId = catalog.EnsureBill(normalized.BillNumber);
            var vote = new Vote
            {
                Id = Guid.NewGuid(),
                SiteId = parsed.SiteId,
                Title = parsed.Title,
                VotedAt = parsed.VotedAt,
                UtcOffsetMinutes = parsed.UtcOffsetMinutes,
                ConvocationId = convocation!.Id,
                BillId = billId,
                Stage = normalized.Stage,
                StageRaw = normalized.StageRaw,
                HasResults = parsed.HasResults,
                Outcome = request.Item.Outcome,
                AcceptPercentSite = request.Item.AcceptPercent,
                Source = VoteSource.Site,
            };

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
            voteWriter.Add(vote);
            existingBySiteId[parsed.SiteId] = new ExistingVoteRow(parsed.SiteId, vote.Outcome, vote.AcceptPercentSite);
            summary.NewVotes++;
        }

        // SaveChanges до материализации голосов: RowId новых голосований/депутатов/периодов присваивает база
        catalog.FlushUpdates();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var deputyVotes = BulkCopyWriter.MaterializeDeputyVotes(pending);
        var statsRows = BulkCopyWriter.BuildFactionStats(deputyVotes, pending, catalog.GetPeriodStatsKeys(), statsService);
        BulkCopyWriter.SortForCopy(deputyVotes, statsRows);
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (deputyVotes.Count > 0)
        {
            await BulkCopyWriter.CopyDeputyVotes(connection, deputyVotes, cancellationToken);
        }

        if (statsRows.Count > 0)
        {
            await BulkCopyWriter.CopyFactionStats(connection, statsRows, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        summary.DeputyVotesImported += deputyVotes.Count;
        summary.FactionStatsImported += statsRows.Count;
        summary.Batches++;
    }

    private static string TruncateReason(string reason)
    {
        return reason.Length <= ImportQuarantineValidationConstants.ReasonMaxLength
            ? reason
            : reason[..ImportQuarantineValidationConstants.ReasonMaxLength];
    }

    private sealed record ExistingVoteRow(long SiteId, VoteOutcome Outcome, decimal? AcceptPercentSite);

    private sealed record BackfillValue(VoteOutcome Outcome, decimal? AcceptPercentSite);

    private sealed record CardRequest(long SiteId, ParsedListingItem Item, Guid ExpectedConvocationId);

    private sealed record CardHtml(CardRequest Request, string? Html);
}