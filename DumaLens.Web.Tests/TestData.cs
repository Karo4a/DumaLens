using DumaLens.Api.Client;

namespace DumaLens.Web.Tests;

/// <summary>
/// Фикстуры данных Api для интеграционных тестов Web (репрезентативные значения)
/// </summary>
internal static class TestData
{
    public static StatsOverviewApiModel Overview()
    {
        return new StatsOverviewApiModel
        {
            TotalVotes = 102320,
            TotalDeputies = 1200,
            TotalFactions = 6,
            TotalBills = 20000,
            DateFrom = new DateTimeOffset(1994, 1, 11, 0, 0, 0, TimeSpan.Zero),
            DateTo = new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero),
        };
    }

    public static ICollection<ConvocationApiModel> Convocations()
    {
        return new List<ConvocationApiModel>
        {
            new() { Number = 8, SiteCode = "8", Title = "VIII созыв", DateFrom = new DateTimeOffset(2021, 10, 12, 0, 0, 0, TimeSpan.Zero), DateTo = null, VoteCount = 17000 },
            new() { Number = 7, SiteCode = "7", Title = "VII созыв", DateFrom = new DateTimeOffset(2016, 10, 5, 0, 0, 0, TimeSpan.Zero), DateTo = new DateTimeOffset(2021, 10, 11, 0, 0, 0, TimeSpan.Zero), VoteCount = 20000 },
        };
    }

    public static ICollection<FactionApiModel> Factions()
    {
        return new List<FactionApiModel>
        {
            Faction("ER", "Единая Россия"),
            Faction("KPRF", "Коммунистическая партия Российской Федерации"),
            Faction("LDPR", "ЛДПР"),
            Faction("SR", "Справедливая Россия"),
            Faction("NL", "Новые люди"),
        };
    }

    public static FactionApiModel Faction(string code, string title)
    {
        return new FactionApiModel
        {
            Code = code,
            Title = title,
            Periods = new List<FactionPeriodApiModel>
            {
                new() { Convocation = 8, SiteId = 72100024, Title = title, DateFrom = new DateTimeOffset(2021, 10, 12, 0, 0, 0, TimeSpan.Zero), DateTo = null },
            },
            ConvocationStats = new List<FactionConvocationStatsApiModel>
            {
                new() { Convocation = 8, VoteCount = 17000, AvgAcceptPctMembers = 85.5, AvgDeclinePctMembers = 3.2, AvgAbstainPctMembers = 0.5, AvgNonePctMembers = 10.8 },
            },
        };
    }

    public static VoteFactionStatsApiModel FactionStat(string code, string title, int member, int accept, int decline, int abstain, int none)
    {
        var voted = accept + decline + abstain;
        return new VoteFactionStatsApiModel
        {
            FactionCode = code,
            FactionTitle = title,
            MemberCount = member,
            VotedCount = voted,
            AcceptCount = accept,
            DeclineCount = decline,
            AbstainCount = abstain,
            NoneCount = none,
            AcceptPctMembers = Pct(accept, member),
            DeclinePctMembers = Pct(decline, member),
            AbstainPctMembers = Pct(abstain, member),
            NonePctMembers = Pct(none, member),
            AcceptPctVoted = Pct(accept, voted),
            DeclinePctVoted = Pct(decline, voted),
            AbstainPctVoted = Pct(abstain, voted),
        };
    }

    public static VoteSearchResultApiModel SearchResultS2()
    {
        var item = new VoteApiModel
        {
            Id = 900001,
            Title = "О внесении изменений в КоАП РФ в части увеличения штрафов за нарушение правил дорожного движения",
            VotedAt = new DateTimeOffset(2024, 3, 12, 13, 40, 0, TimeSpan.FromHours(3)),
            Convocation = 8,
            BillNumber = "900001-8",
            Stage = VoteStage.SecondReadingRejectedAmendments,
            Outcome = VoteOutcome.Accepted,
            AcceptCount = 350,
            DeclineCount = 60,
            AbstainCount = 5,
            NoneCount = 35,
            HasResults = true,
            FactionStats = new List<VoteFactionStatsApiModel>
            {
                FactionStat("ER", "Единая Россия", 324, 295, 3, 0, 26),
                FactionStat("KPRF", "КПРФ", 57, 5, 36, 0, 16),
                FactionStat("NL", "Новые люди", 15, 12, 1, 0, 2),
            },
        };
        return new VoteSearchResultApiModel { TotalCount = 223, Page = 1, PageSize = 20, Items = new List<VoteApiModel> { item } };
    }

    public static VoteDetailsApiModel VoteDetails126282()
    {
        return new VoteDetailsApiModel
        {
            Id = 126282,
            Title = "(третье чтение) О проекте федерального закона № 500091-8 «О внесении изменений в статью 29 Федерального закона „О государственном регулировании производства и оборота этилового спирта, алкогольной и спиртосодержащей продукции“»",
            VotedAt = new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(3)),
            Convocation = 8,
            BillNumber = "500091-8",
            Stage = VoteStage.ThirdReading,
            Outcome = VoteOutcome.Accepted,
            AcceptCount = 396,
            DeclineCount = 0,
            AbstainCount = 0,
            NoneCount = 51,
            HasResults = true,
            Source = VoteSource.Archive,
            FactionStats = new List<VoteFactionStatsApiModel>
            {
                FactionStat("ER", "Единая Россия", 324, 289, 0, 0, 35),
                FactionStat("KPRF", "КПРФ", 57, 54, 0, 0, 3),
                FactionStat("NL", "Новые люди", 15, 12, 0, 0, 3),
            },
            DeputyVoteGroups = new List<DeputyVoteGroupApiModel>
            {
                new()
                {
                    FactionCode = "ER",
                    FactionTitle = "Единая Россия",
                    Deputies = new List<DeputyVoteApiModel>
                    {
                        new() { Id = 99113478, FullName = "Абакаров Хизри Магомедович", Result = VoteResult.Accept },
                        new() { Id = 99113479, FullName = "Аверов Дмитрий Львович", Result = VoteResult.None },
                    },
                },
            },
        };
    }

    public static BillApiModel Bill500091()
    {
        return new BillApiModel
        {
            Number = "500091-8",
            SozdUrl = "https://sozd.duma.gov.ru/bill/500091-8",
            Votes = new List<BillVoteApiModel>
            {
                new()
                {
                    Id = 126282,
                    VotedAt = new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(3)),
                    Stage = VoteStage.ThirdReading,
                    Outcome = VoteOutcome.Accepted,
                    FactionPositions = new List<BillFactionPositionApiModel>
                    {
                        new() { FactionCode = "ER", FactionTitle = "Единая Россия", AcceptPctMembers = 89.2, DeclinePctMembers = 0, AbstainPctMembers = 0, NonePctMembers = 10.8 },
                    },
                },
            },
        };
    }

    public static DeputyApiModel Deputy()
    {
        return new DeputyApiModel
        {
            Id = 99113478,
            FullName = "Абакаров Хизри Магомедович",
            SortName = "Абакаров Хизри Магомедович",
            TotalVotes = 17000,
            AcceptCount = 15000,
            DeclineCount = 200,
            AbstainCount = 50,
            NoneCount = 1750,
            ParticipationPct = 89.7,
            Factions = new List<DeputyMembershipApiModel>
            {
                new() { Convocation = 8, FactionCode = "ER", FactionTitle = "Единая Россия", PeriodTitle = "Единая Россия", DateFrom = new DateTimeOffset(2021, 10, 12, 0, 0, 0, TimeSpan.Zero), DateTo = null },
            },
        };
    }

    public static ICollection<DeputyListItemApiModel> DeputyList()
    {
        return new List<DeputyListItemApiModel>
        {
            new() { Id = 99113478, FullName = "Абакаров Хизри Магомедович", SortName = "Абакаров Хизри Магомедович", FactionCodes = new List<string> { "ER" } },
        };
    }

    private static double? Pct(int count, int total)
    {
        return total > 0 ? Math.Round((double)count / total * 100, 2) : null;
    }
}