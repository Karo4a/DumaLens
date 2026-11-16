using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.Contracts.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.Search;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IVoteReadRepository"/>
public class VoteReadRepository : IVoteReadRepository, IRepositoryAnchor
{
    /// <summary>
    /// Конфигурация полнотекстового поиска (русская морфология, ADR-1)
    /// </summary>
    private const string TextSearchConfig = "russian";

    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    async Task<VoteSearchPageDbModel> IVoteReadRepository.Search(VoteSearchSpec spec, CancellationToken cancellationToken)
    {
        var query = reader.Read<Vote>().NotDeletedAt();
        // FTS-функция строится ВНУТРИ дерева выражений: вызов EF.Functions вне лямбды
        // выполняется на клиенте и бросает InvalidOperationException (client-evaluation).
        // Префиксные лексемы (алкоголь:* → алкогол:*) объединяют существительные и прилагательные (П-4)
        var tsQueryText = RussianTsQueryBuilder.Build(spec.Query);
        if (tsQueryText != null)
        {
            query = query.Where(x => EF.Property<NpgsqlTsVector>(x, "TitleSearch").Matches(EF.Functions.ToTsQuery(TextSearchConfig, tsQueryText)));
        }
        if (spec.ConvocationId is { } convocationId)
        {
            query = query.Where(x => x.ConvocationId == convocationId);
        }
        if (spec.BillId is { } billId)
        {
            query = query.Where(x => x.BillId == billId);
        }
        if (spec.Stage is { } stage)
        {
            query = query.Where(x => x.Stage == stage);
        }
        if (spec.Outcome is { } outcome)
        {
            query = query.Where(x => x.Outcome == outcome);
        }
        if (spec.HasResults is { } hasResults)
        {
            query = query.Where(x => x.HasResults == hasResults);
        }
        // Дата голосования — московская: сравниваем мгновенное время, сдвинутое на историческое смещение,
        // с границами суток (точное включение обеих границ независимо от смещения +03:00/+04:00)
        if (spec.DateFrom is { } dateFrom)
        {
            var from = new DateTimeOffset(dateFrom.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.VotedAt.AddMinutes(x.UtcOffsetMinutes) >= from);
        }
        if (spec.DateTo is { } dateTo)
        {
            var to = new DateTimeOffset(dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.VotedAt.AddMinutes(x.UtcOffsetMinutes) < to);
        }
        var stats = reader.Read<VoteFactionStats>();
        foreach (var condition in spec.FactionConditions)
        {
            query = ApplyFactionCondition(query, stats, condition);
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, spec.Sort, tsQueryText)
            .Skip((spec.Page - 1) * spec.PageSize)
            .Take(spec.PageSize)
            .Select(x => new VoteSearchDbModel
            {
                Id = x.Id,
                SiteId = x.SiteId,
                Title = x.Title,
                VotedAt = x.VotedAt,
                UtcOffsetMinutes = x.UtcOffsetMinutes,
                ConvocationNumber = x.Convocation.Number,
                BillNumber = x.Bill != null ? x.Bill.Number : null,
                Stage = x.Stage,
                Outcome = x.Outcome,
                AcceptCount = x.AcceptCountTotal,
                DeclineCount = x.DeclineCountTotal,
                AbstainCount = x.AbstainCountTotal,
                NoneCount = x.NoneCountTotal,
                HasResults = x.HasResults,
            })
            .ToListAsync(cancellationToken);
        await AttachFactionStats(items, cancellationToken);
        return new VoteSearchPageDbModel
        {
            TotalCount = totalCount,
            Page = spec.Page,
            PageSize = spec.PageSize,
            Items = items,
        };
    }

    /// <summary>
    /// Добавляет условие EXISTS по фракционной строке: результат, база и оператор задают колонку процента и сравнение
    /// </summary>
    /// <remarks>
    /// NULL-процент (пустой состав, никто не голосовал) сравнению не удовлетворяет — ни в SQL, ни в LINQ to Objects.
    /// Явный перебор колонок вместо composition выражений: каждая ветка гарантированно транслируется в SQL.
    /// </remarks>
    async Task<VoteDetailsDbModel?> IVoteReadRepository.GetDetailsBySiteId(long siteId, CancellationToken cancellationToken)
    {
        var vote = await reader.Read<Vote>()
            .NotDeletedAt()
            .Where(x => x.SiteId == siteId)
            .Select(x => new VoteDetailsDbModel
            {
                Id = x.Id,
                RowId = x.RowId,
                SiteId = x.SiteId,
                Title = x.Title,
                VotedAt = x.VotedAt,
                UtcOffsetMinutes = x.UtcOffsetMinutes,
                ConvocationNumber = x.Convocation.Number,
                BillNumber = x.Bill != null ? x.Bill.Number : null,
                Stage = x.Stage,
                StageRaw = x.StageRaw,
                Outcome = x.Outcome,
                AcceptCount = x.AcceptCountTotal,
                DeclineCount = x.DeclineCountTotal,
                AbstainCount = x.AbstainCountTotal,
                NoneCount = x.NoneCountTotal,
                AcceptPercentSite = x.AcceptPercentSite,
                HasResults = x.HasResults,
                Source = x.Source,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (vote == null)
        {
            return null;
        }
        await AttachFactionStats(new List<VoteSearchDbModel> { vote }, cancellationToken);
        vote.DeputyVotes = await reader.Read<DeputyVote>()
            .Where(x => x.VoteId == vote.RowId)
            .Select(x => new DeputyVoteDbModel
            {
                DeputySiteId = x.Deputy.SiteId,
                FullName = x.Deputy.FullName,
                SortName = x.Deputy.SortName,
                FactionCode = x.FactionPeriod != null ? x.FactionPeriod.Faction.Code : null,
                FactionTitle = x.FactionPeriod != null ? x.FactionPeriod.Title : null,
                Result = x.Result,
            })
            .OrderBy(x => x.SortName)
            .ThenBy(x => x.DeputySiteId)
            .ToReadOnlyCollectionAsync(cancellationToken);
        return vote;
    }

    private static IQueryable<Vote> ApplyFactionCondition(IQueryable<Vote> query, IQueryable<VoteFactionStats> stats, VoteFactionConditionSpec condition)
    {
        var factionId = condition.FactionId;
        var threshold = condition.Threshold;
        return (condition.Result, condition.Basis, condition.Operator) switch
        {
            (VoteResult.Accept, PercentBasis.Members, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AcceptPctMembers >= threshold)),
            (VoteResult.Accept, PercentBasis.Members, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AcceptPctMembers <= threshold)),
            (VoteResult.Accept, PercentBasis.Voted, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AcceptPctVoted >= threshold)),
            (VoteResult.Accept, PercentBasis.Voted, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AcceptPctVoted <= threshold)),
            (VoteResult.Decline, PercentBasis.Members, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.DeclinePctMembers >= threshold)),
            (VoteResult.Decline, PercentBasis.Members, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.DeclinePctMembers <= threshold)),
            (VoteResult.Decline, PercentBasis.Voted, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.DeclinePctVoted >= threshold)),
            (VoteResult.Decline, PercentBasis.Voted, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.DeclinePctVoted <= threshold)),
            (VoteResult.Abstain, PercentBasis.Members, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AbstainPctMembers >= threshold)),
            (VoteResult.Abstain, PercentBasis.Members, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AbstainPctMembers <= threshold)),
            (VoteResult.Abstain, PercentBasis.Voted, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AbstainPctVoted >= threshold)),
            (VoteResult.Abstain, PercentBasis.Voted, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.AbstainPctVoted <= threshold)),
            (VoteResult.None, PercentBasis.Members, ComparisonOperator.Gte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.NonePctMembers >= threshold)),
            (VoteResult.None, PercentBasis.Members, ComparisonOperator.Lte) => query.Where(x => stats.Any(s => s.VoteId == x.Id && s.FactionId == factionId && s.NonePctMembers <= threshold)),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "Недопустимое сочетание результата, базы и оператора (none+voted запрещено контрактом)"),
        };
    }

    /// <summary>
    /// Применяет сортировку; при равенстве ключа — по идентификатору (стабильная пагинация)
    /// </summary>
    private static IQueryable<Vote> ApplySort(IQueryable<Vote> query, VoteSearchSort sort, string? tsQueryText)
        => sort switch
        {
            VoteSearchSort.DateAsc => query.OrderBy(x => x.VotedAt).ThenBy(x => x.Id),
            VoteSearchSort.Relevance when tsQueryText != null => query.OrderByDescending(x => EF.Property<NpgsqlTsVector>(x, "TitleSearch").Rank(EF.Functions.ToTsQuery(TextSearchConfig, tsQueryText))).ThenBy(x => x.Id),
            VoteSearchSort.AcceptCountDesc => query.OrderByDescending(x => x.AcceptCountTotal).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.VotedAt).ThenBy(x => x.Id),
        };

    /// <summary>
    /// Догружает фракционные строки страницы отдельным запросом по идентификаторам (проекции вместо Include)
    /// </summary>
    private async Task AttachFactionStats(List<VoteSearchDbModel> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }
        var voteIds = items.Select(x => x.Id).ToList();
        var stats = await reader.Read<VoteFactionStats>()
            .Where(x => voteIds.Contains(x.VoteId))
            .Select(x => new VoteFactionStatsDbModel
            {
                VoteId = x.VoteId,
                FactionId = x.FactionId,
                FactionCode = x.Faction.Code,
                FactionTitle = x.Faction.Title,
                MemberCount = x.MemberCount,
                VotedCount = x.VotedCount,
                AcceptCount = x.AcceptCount,
                DeclineCount = x.DeclineCount,
                AbstainCount = x.AbstainCount,
                NoneCount = x.NoneCount,
                AcceptPctMembers = x.AcceptPctMembers,
                DeclinePctMembers = x.DeclinePctMembers,
                AbstainPctMembers = x.AbstainPctMembers,
                NonePctMembers = x.NonePctMembers,
                AcceptPctVoted = x.AcceptPctVoted,
                DeclinePctVoted = x.DeclinePctVoted,
                AbstainPctVoted = x.AbstainPctVoted,
            })
            .OrderBy(x => x.FactionCode)
            .ThenBy(x => x.FactionId)
            .ToListAsync(cancellationToken);
        var statsByVote = stats
            .GroupBy(x => x.VoteId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<VoteFactionStatsDbModel>)x.ToList());
        foreach (var item in items)
        {
            item.FactionStats = statsByVote.TryGetValue(item.Id, out var rows) ? rows : [];
        }
    }
}