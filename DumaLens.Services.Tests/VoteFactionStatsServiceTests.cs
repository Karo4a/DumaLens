using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Services.Contracts;
using DumaLens.Services.Stats;
using FluentAssertions;
using System.Text.Json.Nodes;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Тесты формул фракционных агрегатов (Э4, 04-functionality.md, §1): обе базы процентов,
/// нулевые знаменатели, пустые голосования, депутаты вне фракций, округление
/// и эталонные ручные расчёты на реальных голосованиях архива.
/// Голоса ссылаются на периоды узкими ключами RowId (ADR-14),
/// агрегаты по-прежнему несут GUID периода и фракции
/// </summary>
public sealed class VoteFactionStatsServiceTests
{
    private readonly VoteFactionStatsService service = new();

    /// <summary>
    /// Обе базы процентов: от состава фракции и от голосовавших (без «не голосовал»)
    /// </summary>
    [Fact]
    public void BuildStatsShouldComputeBothBases()
    {
        var voteId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var factionId = Guid.NewGuid();
        const short periodRowId = 1;
        var deputyVotes = new List<DeputyVote>();
        for (var i = 0; i < 12; i++)
        {
            deputyVotes.Add(CreateDeputyVote(periodRowId, VoteResult.Accept));
        }

        for (var i = 0; i < 3; i++)
        {
            deputyVotes.Add(CreateDeputyVote(periodRowId, VoteResult.None));
        }

        var stats = service.BuildStats(voteId, deputyVotes, new Dictionary<short, FactionPeriodStatsKey>
        {
            [periodRowId] = new FactionPeriodStatsKey(periodId, factionId),
        });

        var row = stats.Should().ContainSingle().Subject;
        row.VoteId.Should().Be(voteId);
        row.FactionPeriodId.Should().Be(periodId);
        row.FactionId.Should().Be(factionId);
        row.AcceptCount.Should().Be(12);
        row.DeclineCount.Should().Be(0);
        row.AbstainCount.Should().Be(0);
        row.NoneCount.Should().Be(3);
        row.MemberCount.Should().Be(15);
        row.VotedCount.Should().Be(12);
        row.AcceptPctMembers.Should().Be(80.00m);
        row.NonePctMembers.Should().Be(20.00m);
        row.DeclinePctMembers.Should().Be(0.00m);
        row.AbstainPctMembers.Should().Be(0.00m);
        row.AcceptPctVoted.Should().Be(100.00m);
        row.DeclinePctVoted.Should().Be(0.00m);
        row.AbstainPctVoted.Should().Be(0.00m);
    }

    /// <summary>
    /// Все депутаты фракции не голосовали: проценты от голосовавших — null, от состава — 0/100
    /// </summary>
    [Fact]
    public void BuildStatsShouldReturnNullPctVotedWhenNobodyVoted()
    {
        var voteId = Guid.NewGuid();
        const short periodRowId = 1;
        var deputyVotes = Enumerable.Range(0, 5).Select(_ => CreateDeputyVote(periodRowId, VoteResult.None)).ToList();

        var stats = service.BuildStats(voteId, deputyVotes, new Dictionary<short, FactionPeriodStatsKey>
        {
            [periodRowId] = new FactionPeriodStatsKey(Guid.NewGuid(), Guid.NewGuid()),
        });

        var row = stats.Should().ContainSingle().Subject;
        row.MemberCount.Should().Be(5);
        row.VotedCount.Should().Be(0);
        row.NonePctMembers.Should().Be(100.00m);
        row.AcceptPctMembers.Should().Be(0.00m);
        row.AcceptPctVoted.Should().BeNull();
        row.DeclinePctVoted.Should().BeNull();
        row.AbstainPctVoted.Should().BeNull();
    }

    /// <summary>
    /// Пустое голосование не даёт строк статистики (04-functionality.md, §1)
    /// </summary>
    [Fact]
    public void BuildStatsShouldReturnEmptyForEmptyVote()
    {
        var stats = service.BuildStats(Guid.NewGuid(), [], new Dictionary<short, FactionPeriodStatsKey>());

        stats.Should().BeEmpty();
    }

    /// <summary>
    /// Депутаты вне фракций в статистику не попадают: знаменатели считаются только по фракции
    /// </summary>
    [Fact]
    public void BuildStatsShouldExcludeDeputiesWithoutFaction()
    {
        var voteId = Guid.NewGuid();
        const short periodRowId = 1;
        var deputyVotes = new List<DeputyVote>
        {
            CreateDeputyVote(periodRowId, VoteResult.Accept),
            CreateDeputyVote(periodRowId, VoteResult.Decline),
            CreateDeputyVote(null, VoteResult.Accept),
            CreateDeputyVote(null, VoteResult.None),
            CreateDeputyVote(null, VoteResult.Abstain),
        };

        var stats = service.BuildStats(voteId, deputyVotes, new Dictionary<short, FactionPeriodStatsKey>
        {
            [periodRowId] = new FactionPeriodStatsKey(Guid.NewGuid(), Guid.NewGuid()),
        });

        var row = stats.Should().ContainSingle().Subject;
        row.MemberCount.Should().Be(2);
        row.VotedCount.Should().Be(2);
        row.AcceptPctMembers.Should().Be(50.00m);
        row.DeclinePctVoted.Should().Be(50.00m);
    }

    /// <summary>
    /// Голосование целиком вне фракций не даёт строк статистики
    /// </summary>
    [Fact]
    public void BuildStatsShouldReturnEmptyWhenAllDeputiesWithoutFaction()
    {
        var voteId = Guid.NewGuid();
        var deputyVotes = Enumerable.Range(0, 3).Select(_ => CreateDeputyVote(null, VoteResult.Accept)).ToList();

        var stats = service.BuildStats(voteId, deputyVotes, new Dictionary<short, FactionPeriodStatsKey>());

        stats.Should().BeEmpty();
    }

    /// <summary>
    /// Разные периоды одной фракции — отдельные строки агрегатов с одним каноническим FactionId
    /// </summary>
    [Fact]
    public void BuildStatsShouldSeparatePeriodsOfSameFaction()
    {
        var voteId = Guid.NewGuid();
        var factionId = Guid.NewGuid();
        var firstPeriodId = Guid.NewGuid();
        var secondPeriodId = Guid.NewGuid();
        const short firstRowId = 1;
        const short secondRowId = 2;
        var deputyVotes = new List<DeputyVote>
        {
            CreateDeputyVote(firstRowId, VoteResult.Accept),
            CreateDeputyVote(firstRowId, VoteResult.Accept),
            CreateDeputyVote(secondRowId, VoteResult.Decline),
            CreateDeputyVote(secondRowId, VoteResult.None),
        };
        var periodKeys = new Dictionary<short, FactionPeriodStatsKey>
        {
            [firstRowId] = new FactionPeriodStatsKey(firstPeriodId, factionId),
            [secondRowId] = new FactionPeriodStatsKey(secondPeriodId, factionId),
        };

        var stats = service.BuildStats(voteId, deputyVotes, periodKeys);

        stats.Should().HaveCount(2);
        stats.Should().OnlyContain(x => x.FactionId == factionId);
        var first = stats.Single(x => x.FactionPeriodId == firstPeriodId);
        first.MemberCount.Should().Be(2);
        first.AcceptPctMembers.Should().Be(100.00m);
        var second = stats.Single(x => x.FactionPeriodId == secondPeriodId);
        second.MemberCount.Should().Be(2);
        second.VotedCount.Should().Be(1);
        second.DeclinePctMembers.Should().Be(50.00m);
        second.NonePctMembers.Should().Be(50.00m);
        second.DeclinePctVoted.Should().Be(100.00m);
    }

    /// <summary>
    /// Округление половины — от нуля: 1/32 = 3.125 → 3.13, 31/32 = 96.875 → 96.88
    /// (числовая база numeric(5,2), половина округляется вверх по модулю)
    /// </summary>
    [Fact]
    public void BuildStatsShouldRoundHalfAwayFromZero()
    {
        var voteId = Guid.NewGuid();
        const short periodRowId = 1;
        var deputyVotes = new List<DeputyVote> { CreateDeputyVote(periodRowId, VoteResult.Accept) };
        deputyVotes.AddRange(Enumerable.Range(0, 31).Select(_ => CreateDeputyVote(periodRowId, VoteResult.None)));

        var stats = service.BuildStats(voteId, deputyVotes, new Dictionary<short, FactionPeriodStatsKey>
        {
            [periodRowId] = new FactionPeriodStatsKey(Guid.NewGuid(), Guid.NewGuid()),
        });

        var row = stats.Should().ContainSingle().Subject;
        row.AcceptPctMembers.Should().Be(3.13m);
        row.NonePctMembers.Should().Be(96.88m);
    }

    /// <summary>
    /// Периодические дроби округляются до двух знаков: 1/3 → 33.33, 2/3 → 66.67
    /// </summary>
    [Fact]
    public void BuildStatsShouldRoundRepeatingFractionsToTwoDigits()
    {
        var voteId = Guid.NewGuid();
        var firstPeriodId = Guid.NewGuid();
        var secondPeriodId = Guid.NewGuid();
        const short firstRowId = 1;
        const short secondRowId = 2;
        var deputyVotes = new List<DeputyVote>
        {
            CreateDeputyVote(firstRowId, VoteResult.Accept),
            CreateDeputyVote(firstRowId, VoteResult.None),
            CreateDeputyVote(firstRowId, VoteResult.None),
            CreateDeputyVote(secondRowId, VoteResult.Accept),
            CreateDeputyVote(secondRowId, VoteResult.Accept),
            CreateDeputyVote(secondRowId, VoteResult.None),
        };
        var periodKeys = new Dictionary<short, FactionPeriodStatsKey>
        {
            [firstRowId] = new FactionPeriodStatsKey(firstPeriodId, Guid.NewGuid()),
            [secondRowId] = new FactionPeriodStatsKey(secondPeriodId, Guid.NewGuid()),
        };

        var stats = service.BuildStats(voteId, deputyVotes, periodKeys);

        var first = stats.Single(x => x.FactionPeriodId == firstPeriodId);
        first.AcceptPctMembers.Should().Be(33.33m);
        first.NonePctMembers.Should().Be(66.67m);
        var second = stats.Single(x => x.FactionPeriodId == secondPeriodId);
        second.AcceptPctMembers.Should().Be(66.67m);
        second.NonePctMembers.Should().Be(33.33m);
    }

    /// <summary>
    /// Эталонный ручной расчёт по голосованию 126282 (13.03.2024, VIII созыв):
    /// 447 депутатов в 6 фракциях; «Вне фракций» — 2 депутата, оба не голосовали
    /// </summary>
    [Fact]
    public void BuildStatsShouldMatchManualReferenceForVote126282()
    {
        var voteId = Guid.NewGuid();
        var (deputyVotes, periodKeys, periodsByFactionSiteId) = LoadFixtureVote("archive_126282");

        var stats = service.BuildStats(voteId, deputyVotes, periodKeys);

        stats.Should().HaveCount(6);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100024, 289, 0, 0, 35, [89.20m, 0.00m, 0.00m, 10.80m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100004, 54, 0, 0, 3, [94.74m, 0.00m, 0.00m, 5.26m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100029, 22, 0, 0, 5, [81.48m, 0.00m, 0.00m, 18.52m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100005, 19, 0, 0, 3, [86.36m, 0.00m, 0.00m, 13.64m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100028, 12, 0, 0, 3, [80.00m, 0.00m, 0.00m, 20.00m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100011, 0, 0, 0, 2, [0.00m, 0.00m, 0.00m, 100.00m], [null, null, null]);
    }

    /// <summary>
    /// Эталонный ручной расчёт по голосованию 18642 (11.01.1994, I созыв):
    /// 444 депутата в 10 фракциях, есть все четыре результата голоса
    /// </summary>
    [Fact]
    public void BuildStatsShouldMatchManualReferenceForVote18642()
    {
        var voteId = Guid.NewGuid();
        var (deputyVotes, periodKeys, periodsByFactionSiteId) = LoadFixtureVote("archive_18642");

        var stats = service.BuildStats(voteId, deputyVotes, periodKeys);

        stats.Should().HaveCount(10);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100002, 63, 0, 3, 12, [80.77m, 0.00m, 3.85m, 15.38m], [95.45m, 0.00m, 4.55m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100014, 51, 2, 1, 12, [77.27m, 3.03m, 1.52m, 18.18m], [94.44m, 3.70m, 1.85m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100005, 60, 0, 1, 3, [93.75m, 0.00m, 1.56m, 4.69m], [98.36m, 0.00m, 1.64m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100001, 50, 0, 0, 5, [90.91m, 0.00m, 0.00m, 9.09m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100004, 40, 2, 1, 2, [88.89m, 4.44m, 2.22m, 4.44m], [93.02m, 4.65m, 2.33m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100011, 36, 0, 3, 3, [85.71m, 0.00m, 7.14m, 7.14m], [92.31m, 0.00m, 7.69m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100006, 28, 1, 0, 1, [93.33m, 3.33m, 0.00m, 3.33m], [96.55m, 3.45m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100008, 23, 2, 0, 1, [88.46m, 7.69m, 0.00m, 3.85m], [92.00m, 8.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100007, 22, 0, 0, 1, [95.65m, 0.00m, 0.00m, 4.35m], [100.00m, 0.00m, 0.00m]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100003, 7, 5, 1, 2, [46.67m, 33.33m, 6.67m, 13.33m], [53.85m, 38.46m, 7.69m]);
    }

    /// <summary>
    /// Эталонный ручной расчёт по голосованию 87506 (13.01.2012, VI созыв):
    /// все 449 депутатов не голосовали — проценты от голосовавших везде null
    /// </summary>
    [Fact]
    public void BuildStatsShouldMatchManualReferenceForVote87506()
    {
        var voteId = Guid.NewGuid();
        var (deputyVotes, periodKeys, periodsByFactionSiteId) = LoadFixtureVote("archive_plus0400");

        var stats = service.BuildStats(voteId, deputyVotes, periodKeys);

        stats.Should().HaveCount(4);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100024, 0, 0, 0, 237, [0.00m, 0.00m, 0.00m, 100.00m], [null, null, null]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100004, 0, 0, 0, 92, [0.00m, 0.00m, 0.00m, 100.00m], [null, null, null]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100027, 0, 0, 0, 64, [0.00m, 0.00m, 0.00m, 100.00m], [null, null, null]);
        AssertStatsRow(stats, periodsByFactionSiteId, 72100005, 0, 0, 0, 56, [0.00m, 0.00m, 0.00m, 100.00m], [null, null, null]);
    }

    /// <summary>
    /// Реальное пустое голосование 17470 (15.11.2002) не даёт строк статистики
    /// </summary>
    [Fact]
    public void BuildStatsShouldReturnEmptyForRealEmptyVote17470()
    {
        var voteId = Guid.NewGuid();
        var (deputyVotes, periodKeys, _) = LoadFixtureVote("archive_empty_17470");

        var stats = service.BuildStats(voteId, deputyVotes, periodKeys);

        deputyVotes.Should().BeEmpty();
        stats.Should().BeEmpty();
    }

    private static DeputyVote CreateDeputyVote(short? periodRowId, VoteResult result)
    {
        return new DeputyVote
        {
            VoteId = 1,
            DeputyId = Random.Shared.Next(1, int.MaxValue),
            FactionPeriodId = periodRowId,
            Result = result,
        };
    }

    /// <summary>
    /// Загружает индивидуальные голоса фикстуры; депутаты без фракции получают FactionPeriodId = null,
    /// каждой фракции соответствует свой период (узкий ключ — порядковый номер) и канонический идентификатор
    /// </summary>
    private static (List<DeputyVote> DeputyVotes, Dictionary<short, FactionPeriodStatsKey> PeriodKeys, Dictionary<long, Guid> PeriodsByFactionSiteId) LoadFixtureVote(string fixtureName)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "json", $"{fixtureName}.json"));
        var node = JsonNode.Parse(json)!;
        var deputyVotes = new List<DeputyVote>();
        var periodKeys = new Dictionary<short, FactionPeriodStatsKey>();
        var periodsByFactionSiteId = new Dictionary<long, Guid>();
        var rowIdsByFactionSiteId = new Dictionary<long, short>();
        var items = node["votes"]?.AsArray() ?? new JsonArray();
        foreach (var item in items)
        {
            short? periodRowId = null;
            var factionNode = item!["deputy"]!["faction"];
            if (factionNode != null)
            {
                var factionSiteId = long.Parse(factionNode["id"]!.ToString());
                if (!periodsByFactionSiteId.TryGetValue(factionSiteId, out var existingPeriodId))
                {
                    existingPeriodId = Guid.NewGuid();
                    periodsByFactionSiteId[factionSiteId] = existingPeriodId;
                    var rowId = (short)periodsByFactionSiteId.Count;
                    rowIdsByFactionSiteId[factionSiteId] = rowId;
                    periodKeys[rowId] = new FactionPeriodStatsKey(existingPeriodId, Guid.NewGuid());
                }

                periodRowId = rowIdsByFactionSiteId[factionSiteId];
            }

            deputyVotes.Add(CreateDeputyVote(periodRowId, ParseResult(item!["result"]!.GetValue<string>())));
        }

        return (deputyVotes, periodKeys, periodsByFactionSiteId);
    }

    private static VoteResult ParseResult(string result)
    {
        return result switch
        {
            "accept" => VoteResult.Accept,
            "decline" => VoteResult.Decline,
            "abstain" => VoteResult.Abstain,
            "none" => VoteResult.None,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Неизвестный результат голоса в фикстуре"),
        };
    }

    private static void AssertStatsRow(
        IReadOnlyCollection<VoteFactionStats> stats,
        IReadOnlyDictionary<long, Guid> periodsByFactionSiteId,
        long factionSiteId,
        int accept,
        int decline,
        int abstain,
        int none,
        decimal?[] pctMembers,
        decimal?[] pctVoted)
    {
        var row = stats.Single(x => x.FactionPeriodId == periodsByFactionSiteId[factionSiteId]);
        row.AcceptCount.Should().Be(accept);
        row.DeclineCount.Should().Be(decline);
        row.AbstainCount.Should().Be(abstain);
        row.NoneCount.Should().Be(none);
        row.MemberCount.Should().Be(accept + decline + abstain + none);
        row.VotedCount.Should().Be(accept + decline + abstain);
        new[] { row.AcceptPctMembers, row.DeclinePctMembers, row.AbstainPctMembers, row.NonePctMembers }.Should().Equal(pctMembers);
        new[] { row.AcceptPctVoted, row.DeclinePctVoted, row.AbstainPctVoted }.Should().Equal(pctVoted);
    }
}