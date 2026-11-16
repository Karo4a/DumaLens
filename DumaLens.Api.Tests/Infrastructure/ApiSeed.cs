using DumaLens.Context;
using DumaLens.Context.Tests;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Детерминированный набор данных для интеграционных тестов API Э6
/// </summary>
public sealed class ApiSeed
{
    private readonly DumaLensContext context;
    private readonly VoteSearchSeed seed;
    private long deputySiteId = 910_000_000;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ApiSeed(DumaLensContext context)
    {
        this.context = context;
        seed = new VoteSearchSeed(context);
    }

    /// <summary>
    /// Седьмой созыв
    /// </summary>
    public Convocation Convocation7 { get; private set; } = null!;

    /// <summary>
    /// Восьмой созыв
    /// </summary>
    public Convocation Convocation8 { get; private set; } = null!;

    /// <summary>
    /// Каноническая фракция Единой России
    /// </summary>
    public Faction Er { get; private set; } = null!;

    /// <summary>
    /// Каноническая фракция КПРФ
    /// </summary>
    public Faction Kprf { get; private set; } = null!;

    /// <summary>
    /// Каноническая фракция Новых людей
    /// </summary>
    public Faction Nl { get; private set; } = null!;

    /// <summary>
    /// Законопроект об алкогольной продукции
    /// </summary>
    public Bill AlcoholBill { get; private set; } = null!;

    /// <summary>
    /// Законопроект о бюджете
    /// </summary>
    public Bill BudgetBill { get; private set; } = null!;

    /// <summary>
    /// Депутат Единой России в седьмом и восьмом созывах
    /// </summary>
    public Deputy Deputy1 { get; private set; } = null!;

    /// <summary>
    /// Депутат КПРФ в восьмом созыве
    /// </summary>
    public Deputy Deputy2 { get; private set; } = null!;

    /// <summary>
    /// Депутат Новых людей в восьмом созыве
    /// </summary>
    public Deputy Deputy3 { get; private set; } = null!;

    /// <summary>
    /// Депутат без фракции
    /// </summary>
    public Deputy Deputy4 { get; private set; } = null!;

    /// <summary>
    /// Голосование по алкогольному законопроекту в третьем чтении (13.03.2024)
    /// </summary>
    public Vote AlcoholThird { get; private set; } = null!;

    /// <summary>
    /// Голосование по алкогольному законопроекту в первом чтении (12.10.2023)
    /// </summary>
    public Vote AlcoholFirst { get; private set; } = null!;

    /// <summary>
    /// Процедурное голосование без законопроекта (20.01.2024)
    /// </summary>
    public Vote ProcedureVote { get; private set; } = null!;

    /// <summary>
    /// Голосование седьмого созыва (01.06.2020)
    /// </summary>
    public Vote BudgetVote { get; private set; } = null!;

    /// <summary>
    /// Голосование по бюджетному законопроекту во втором чтении, отклонено (01.02.2024)
    /// </summary>
    public Vote BudgetSecond { get; private set; } = null!;

    /// <summary>
    /// Пустое голосование без результатов (01.04.2024)
    /// </summary>
    public Vote EmptyVote { get; private set; } = null!;

    /// <summary>
    /// Заполняет базу детерминированным набором данных
    /// </summary>
    public async Task SeedAsync()
    {
        Convocation7 = await seed.GetOrAddConvocation(7);
        Convocation8 = await seed.GetOrAddConvocation(8);
        Er = await seed.GetOrAddFaction("ER", "Единая Россия");
        Kprf = await seed.GetOrAddFaction("KPRF", "КПРФ");
        Nl = await seed.GetOrAddFaction("NL", "Новые люди");
        var er7 = await seed.GetOrAddPeriod(Er, Convocation7, "Всероссийская политическая партия «ЕДИНАЯ РОССИЯ»");
        var er8 = await seed.GetOrAddPeriod(Er, Convocation8, "Фракция «ЕДИНАЯ РОССИЯ»");
        var kprf7 = await seed.GetOrAddPeriod(Kprf, Convocation7, "Фракция КПРФ");
        var kprf8 = await seed.GetOrAddPeriod(Kprf, Convocation8, "Фракция КПРФ");
        var nl8 = await seed.GetOrAddPeriod(Nl, Convocation8, "Фракция «НОВЫЕ ЛЮДИ»");
        AlcoholBill = await seed.GetOrAddBill("500091-8");
        BudgetBill = await seed.GetOrAddBill("123456-8");

        Deputy1 = await AddDeputy("Иванов Иван Иванович");
        Deputy2 = await AddDeputy("Петров Пётр Петрович");
        Deputy3 = await AddDeputy("Сидоров Сидор Сидорович");
        Deputy4 = await AddDeputy("Безфракционный Борис Борисович");
        await AddMembership(Deputy1, er7);
        await AddMembership(Deputy1, er8);
        await AddMembership(Deputy2, kprf8);
        await AddMembership(Deputy3, nl8);

        AlcoholThird = await seed.AddVote(
            Convocation8,
            "О проекте федерального закона № 500091-8 «Об алкогольной продукции» (третье чтение)",
            new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(3)),
            stage: VoteStage.ThirdReading,
            outcome: VoteOutcome.Accepted,
            acceptCount: 3,
            declineCount: 1,
            noneCount: 1,
            bill: AlcoholBill);
        await seed.AddStats(AlcoholThird, er8, 2, 0, 0, 0);
        await seed.AddStats(AlcoholThird, kprf8, 0, 1, 0, 0);
        await seed.AddStats(AlcoholThird, nl8, 1, 0, 0, 0);
        await AddDeputyVote(AlcoholThird, Deputy1, er8, VoteResult.Accept);
        await AddDeputyVote(AlcoholThird, Deputy2, kprf8, VoteResult.Decline);
        await AddDeputyVote(AlcoholThird, Deputy3, nl8, VoteResult.Accept);
        await AddDeputyVote(AlcoholThird, Deputy4, null, VoteResult.None);

        AlcoholFirst = await seed.AddVote(
            Convocation8,
            "О проекте федерального закона № 500091-8 «Об алкогольной продукции» (первое чтение)",
            new DateTimeOffset(2023, 10, 12, 10, 0, 0, TimeSpan.FromHours(3)),
            stage: VoteStage.FirstReading,
            outcome: VoteOutcome.Accepted,
            acceptCount: 2,
            declineCount: 1,
            abstainCount: 1,
            bill: AlcoholBill);
        await seed.AddStats(AlcoholFirst, er8, 2, 0, 0, 0);
        await seed.AddStats(AlcoholFirst, kprf8, 0, 1, 0, 0);
        await seed.AddStats(AlcoholFirst, nl8, 0, 0, 1, 0);
        await AddDeputyVote(AlcoholFirst, Deputy1, er8, VoteResult.Accept);
        await AddDeputyVote(AlcoholFirst, Deputy2, kprf8, VoteResult.Decline);
        await AddDeputyVote(AlcoholFirst, Deputy3, nl8, VoteResult.Abstain);
        await AddDeputyVote(AlcoholFirst, Deputy4, null, VoteResult.None);

        ProcedureVote = await seed.AddVote(
            Convocation8,
            "О порядке работы Государственной Думы",
            new DateTimeOffset(2024, 1, 20, 12, 0, 0, TimeSpan.FromHours(3)),
            stage: VoteStage.Procedural,
            outcome: VoteOutcome.Unknown,
            acceptCount: 4);

        BudgetVote = await seed.AddVote(
            Convocation7,
            "О проекте федерального закона № 123456-7 «О федеральном бюджете» (третье чтение)",
            new DateTimeOffset(2020, 6, 1, 11, 0, 0, TimeSpan.FromHours(3)),
            stage: VoteStage.ThirdReading,
            outcome: VoteOutcome.Accepted,
            acceptCount: 300,
            declineCount: 50,
            abstainCount: 10,
            noneCount: 90);
        await seed.AddStats(BudgetVote, er7, 250, 20, 5, 75);
        await seed.AddStats(BudgetVote, kprf7, 30, 25, 5, 10);

        BudgetSecond = await seed.AddVote(
            Convocation8,
            "О проекте федерального закона № 123456-8 «О федеральном бюджете» (второе чтение)",
            new DateTimeOffset(2024, 2, 1, 14, 0, 0, TimeSpan.FromHours(3)),
            stage: VoteStage.SecondReading,
            outcome: VoteOutcome.Rejected,
            acceptCount: 1,
            declineCount: 3,
            bill: BudgetBill);
        await seed.AddStats(BudgetSecond, er8, 1, 1, 0, 0);
        await seed.AddStats(BudgetSecond, kprf8, 0, 2, 0, 0);

        EmptyVote = await seed.AddVote(
            Convocation8,
            "О проекте федерального закона № 999999-8 «О пустом голосовании»",
            new DateTimeOffset(2024, 4, 1, 9, 0, 0, TimeSpan.FromHours(3)),
            stage: VoteStage.Unknown,
            outcome: VoteOutcome.Unknown,
            hasResults: false);
    }

    private async Task<Deputy> AddDeputy(string fullName)
    {
        var deputy = new Deputy
        {
            SiteId = deputySiteId++,
            FullName = fullName,
            SortName = fullName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        context.Add(deputy);
        await context.SaveChangesAsync();
        return deputy;
    }

    private async Task AddMembership(Deputy deputy, FactionPeriod period)
    {
        var membership = new DeputyFactionMembership
        {
            DeputyId = deputy.Id,
            FactionPeriodId = period.Id,
            DateFrom = null,
            DateTo = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        context.Add(membership);
        await context.SaveChangesAsync();
    }

    private async Task AddDeputyVote(Vote vote, Deputy deputy, FactionPeriod? period, VoteResult result)
    {
        var deputyVote = new DeputyVote
        {
            VoteId = vote.RowId,
            DeputyId = deputy.RowId,
            FactionPeriodId = period?.RowId,
            Result = result,
        };
        context.Add(deputyVote);
        await context.SaveChangesAsync();
    }
}