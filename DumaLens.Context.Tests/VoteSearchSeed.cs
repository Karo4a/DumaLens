using DumaLens.Context;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Context.Tests;

/// <summary>
/// Наполнение базы сценариями поиска голосований (Э5)
/// </summary>
/// <remarks>
/// Справочники создаются или переиспользуются: на PostgreSQL миграция уже содержит seed созывов,
/// фракций и периодов, на InMemory база пуста. Голосования и фракционные агрегаты — всегда новые.
/// Проценты считаются формулами Э4 (округление до 2 знаков, половина — от нуля; null при нулевом знаменателе).
/// </remarks>
public sealed class VoteSearchSeed
{
    private readonly DumaLensContext context;
    private long siteId = 900_000_000;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteSearchSeed(DumaLensContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Возвращает созыв с заданным номером (существующий или новый)
    /// </summary>
    public async Task<Convocation> GetOrAddConvocation(int number)
    {
        var existing = await context.Set<Convocation>().FirstOrDefaultAsync(x => x.Number == number);
        if (existing != null)
        {
            return existing;
        }
        var convocation = new Convocation
        {
            Number = number,
            SiteCode = $"TEST{number}",
            DateFrom = new DateOnly(1994, 1, 1),
            DateTo = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        context.Add(convocation);
        await context.SaveChangesAsync();
        return convocation;
    }

    /// <summary>
    /// Возвращает каноническую фракцию с заданным кодом (существующую или новую)
    /// </summary>
    public async Task<Faction> GetOrAddFaction(string code, string title)
    {
        var existing = await context.Set<Faction>().FirstOrDefaultAsync(x => x.Code == code);
        if (existing != null)
        {
            return existing;
        }
        var faction = new Faction
        {
            Code = code,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        context.Add(faction);
        await context.SaveChangesAsync();
        return faction;
    }

    /// <summary>
    /// Возвращает период фракции в созыве, приводя его название к заданному (историческому)
    /// </summary>
    public async Task<FactionPeriod> GetOrAddPeriod(Faction faction, Convocation convocation, string title)
    {
        var existing = await context.Set<FactionPeriod>()
            .FirstOrDefaultAsync(x => x.FactionId == faction.Id && x.ConvocationId == convocation.Id);
        if (existing != null)
        {
            if (existing.Title != title)
            {
                existing.Title = title;
                context.Update(existing);
                await context.SaveChangesAsync();
            }
            return existing;
        }
        var period = new FactionPeriod
        {
            FactionId = faction.Id,
            ConvocationId = convocation.Id,
            SiteId = NextSiteId(),
            Title = title,
            DateFrom = convocation.DateFrom,
            DateTo = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        context.Add(period);
        await context.SaveChangesAsync();
        return period;
    }

    /// <summary>
    /// Возвращает законопроект с заданным номером (существующий или новый)
    /// </summary>
    public async Task<Bill> GetOrAddBill(string number, DateTimeOffset? deletedAt = null)
    {
        var existing = await context.Set<Bill>().FirstOrDefaultAsync(x => x.Number == number);
        if (existing != null)
        {
            return existing;
        }
        var bill = new Bill
        {
            Number = number,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = deletedAt,
        };
        context.Add(bill);
        await context.SaveChangesAsync();
        return bill;
    }

    /// <summary>
    /// Добавляет голосование; <paramref name="votedAt"/> — мгновенное время UTC (конвенция хранения),
    /// московское время = UTC + <paramref name="utcOffsetMinutes"/>
    /// </summary>
    public async Task<Vote> AddVote(
        Convocation convocation,
        string title,
        DateTimeOffset votedAt,
        int utcOffsetMinutes = 180,
        VoteStage stage = VoteStage.ThirdReading,
        VoteOutcome outcome = VoteOutcome.Accepted,
        int acceptCount = 0,
        int declineCount = 0,
        int abstainCount = 0,
        int noneCount = 0,
        Bill? bill = null,
        bool hasResults = true,
        DateTimeOffset? deletedAt = null)
    {
        var vote = new Vote
        {
            SiteId = NextSiteId(),
            Title = title,
            VotedAt = votedAt,
            UtcOffsetMinutes = utcOffsetMinutes,
            ConvocationId = convocation.Id,
            BillId = bill?.Id,
            Stage = stage,
            StageRaw = null,
            Outcome = outcome,
            AcceptCountTotal = acceptCount,
            DeclineCountTotal = declineCount,
            AbstainCountTotal = abstainCount,
            NoneCountTotal = noneCount,
            AcceptPercentSite = null,
            HasResults = hasResults,
            Source = VoteSource.Archive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = deletedAt,
        };
        context.Add(vote);
        await context.SaveChangesAsync();
        return vote;
    }

    /// <summary>
    /// Добавляет фракционную строку голосования: счётчики и проценты по формулам Э4
    /// </summary>
    public async Task<VoteFactionStats> AddStats(Vote vote, FactionPeriod period, int acceptCount, int declineCount, int abstainCount, int noneCount)
    {
        var memberCount = acceptCount + declineCount + abstainCount + noneCount;
        var votedCount = acceptCount + declineCount + abstainCount;
        var stats = new VoteFactionStats
        {
            VoteId = vote.Id,
            FactionPeriodId = period.Id,
            FactionId = period.FactionId,
            AcceptCount = acceptCount,
            DeclineCount = declineCount,
            AbstainCount = abstainCount,
            NoneCount = noneCount,
            MemberCount = memberCount,
            VotedCount = votedCount,
            AcceptPctMembers = Percent(acceptCount, memberCount),
            DeclinePctMembers = Percent(declineCount, memberCount),
            AbstainPctMembers = Percent(abstainCount, memberCount),
            NonePctMembers = Percent(noneCount, memberCount),
            AcceptPctVoted = Percent(acceptCount, votedCount),
            DeclinePctVoted = Percent(declineCount, votedCount),
            AbstainPctVoted = Percent(abstainCount, votedCount),
        };
        context.Add(stats);
        await context.SaveChangesAsync();
        return stats;
    }

    /// <summary>
    /// Выделяет уникальный site-id вне диапазона реальных данных и seed
    /// </summary>
    private long NextSiteId()
        => siteId++;

    /// <summary>
    /// Процент с округлением Э4; null при нулевом знаменателе
    /// </summary>
    private static decimal? Percent(int count, int total)
        => total == 0 ? null : Math.Round(count * 100m / total, 2, MidpointRounding.AwayFromZero);
}