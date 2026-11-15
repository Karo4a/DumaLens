using AutoMapper;
using DumaLens.Context;
using DumaLens.Context.Tests;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.ReadRepositories;
using DumaLens.Repositories.Tests.Infrastructure;
using DumaLens.Services;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Infrastructure;
using DumaLens.Services.Search;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests.Integration;

/// <summary>
/// Интеграционные тесты поиска голосований (Э5) на одноразовой PostgreSQL-базе:
/// полнотекстовый поиск (ADR-1), EXISTS-условия по фракциям, сценарии §3.4 ТЗ,
/// даты с историческим смещением +04:00, сверка процентов с ручным расчётом (П-3).
/// Каждый тест получает собственную базу (изоляция мутирующих сценариев)
/// </summary>
public sealed class VoteSearchIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture fixture = new();
    private DumaLensContext? context;
    private IVoteSearchService service = null!;
    private VoteSearchSeed seed = null!;

    /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
    public async Task InitializeAsync()
    {
        await fixture.InitializeAsync();
        if (fixture.SkipReason != null)
        {
            return;
        }
        context = fixture.CreateContext();
        var configuration = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>());
        service = new VoteSearchService(
            new ValidateService(),
            new VoteReadRepository(context),
            new ConvocationReadRepository(context),
            new BillReadRepository(context),
            new FactionReadRepository(context),
            configuration.CreateMapper());
        seed = new VoteSearchSeed(context);
    }

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
    public async Task DisposeAsync()
    {
        if (context != null)
        {
            await context.DisposeAsync();
        }
        await fixture.DisposeAsync();
    }

    /// <summary>
    /// FTS: русская морфология, И слов, минус-слово, фразовый поиск с учётом порядка
    /// </summary>
    [SkippableFact]
    public async Task SearchFtsShouldMatchRussianMorphologyAndOperators()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        // Префиксные лексемы (алкоголь:* → алкогол:*) находят и падежные формы, и прилагательные
        // (алкогольной → алкогольн); websearch_to_tsquery этого не делал — см. RussianTsQueryBuilder
        var night = await seed.AddVote(convocation, "О запрете розничной продажи алкоголя в ночное время", MoscowTime(2024, 5, 10, 12, 0, 3));
        var minors = await seed.AddVote(convocation, "Об ограничении продажи алкоголя несовершеннолетним", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddVote(convocation, "О налоге на добавленную стоимость", MoscowTime(2024, 5, 12, 12, 0, 3));

        var morphology = await service.Search(new VoteSearchQueryModel { Query = "алкоголь" }, CancellationToken.None);
        var bothWords = await service.Search(new VoteSearchQueryModel { Query = "алкоголь розничной" }, CancellationToken.None);
        var minus = await service.Search(new VoteSearchQueryModel { Query = "алкоголь -несовершеннолетним" }, CancellationToken.None);
        var phrase = await service.Search(new VoteSearchQueryModel { Query = "\"розничной продажи\"" }, CancellationToken.None);
        var reversedPhrase = await service.Search(new VoteSearchQueryModel { Query = "\"продажи розничной\"" }, CancellationToken.None);

        morphology.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { night.Id, minors.Id }, "морфология существительного: алкоголя → алкогол");
        bothWords.Items.Should().ContainSingle().Which.Id.Should().Be(night.Id, "И слов; прилагательное розничной → розничн с обеих сторон");
        minus.Items.Should().ContainSingle().Which.Id.Should().Be(night.Id);
        phrase.Items.Should().ContainSingle().Which.Id.Should().Be(night.Id);
        reversedPhrase.Items.Should().BeEmpty("фразовый поиск учитывает порядок слов");
    }

    /// <summary>
    /// П-4: поиск по существительному находит заголовки с прилагательным
    /// (префиксные лексемы: алкогол:* матчит и алкогол, и алкогольн)
    /// </summary>
    [SkippableFact]
    public async Task SearchFtsPrefixShouldMatchAdjectiveForms()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var adjective = await seed.AddVote(convocation, "О государственном регулировании оборота алкогольной продукции", MoscowTime(2024, 5, 10, 12, 0, 3));
        var noun = await seed.AddVote(convocation, "Об ограничении продажи алкоголя", MoscowTime(2024, 5, 11, 12, 0, 3));

        var result = await service.Search(new VoteSearchQueryModel { Query = "алкоголь" }, CancellationToken.None);

        result.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { adjective.Id, noun.Id });
    }

    /// <summary>
    /// Сортировка по релевантности выполняется на PostgreSQL и возвращает совпавшие голосования
    /// </summary>
    [SkippableFact]
    public async Task SearchFtsRelevanceSortShouldExecute()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var first = await seed.AddVote(convocation, "Об ограничении продажи алкоголя", MoscowTime(2024, 5, 10, 12, 0, 3));
        var second = await seed.AddVote(convocation, "О запрете розничной продажи алкоголя", MoscowTime(2024, 5, 11, 12, 0, 3));

        var result = await service.Search(new VoteSearchQueryModel { Query = "алкоголь", Sort = "relevance" }, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
    }

    /// <summary>
    /// FTS комбинируется со структурными фильтрами по И
    /// </summary>
    [SkippableFact]
    public async Task SearchFtsCombinedWithStageFilterShouldIntersect()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "О запрете продажи алкоголя в ночное время", MoscowTime(2024, 5, 10, 12, 0, 3), stage: VoteStage.ThirdReading);
        var firstReading = await seed.AddVote(convocation, "Об ограничении продажи алкоголя", MoscowTime(2024, 5, 11, 12, 0, 3), stage: VoteStage.FirstReading);

        var result = await service.Search(new VoteSearchQueryModel { Query = "алкоголь", Stage = "firstReading" }, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(firstReading.Id);
    }

    /// <summary>
    /// Сценарий §3.4 №1: «Новые люди» за ≥ 70% от состава
    /// </summary>
    [SkippableFact]
    public async Task SearchScenario1NlAcceptGte70()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, nlPeriod) = await SeedFaction("NL", "Новые люди", convocation, "НЛ");
        var (_, erPeriod) = await SeedFaction("ER", "Единая Россия", convocation);
        var matching = await seed.AddVote(convocation, "Подходит", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(matching, nlPeriod, 12, 0, 0, 3);
        await seed.AddStats(matching, erPeriod, 280, 20, 5, 15);
        var notMatching = await seed.AddVote(convocation, "Не подходит", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(notMatching, nlPeriod, 2, 1, 0, 0);
        var noStats = await seed.AddVote(convocation, "Без строки НЛ", MoscowTime(2024, 5, 12, 12, 0, 3));
        await seed.AddStats(noStats, erPeriod, 300, 0, 0, 20);

        var result = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("НЛ", "accept", 70m)] }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(matching.Id);
    }

    /// <summary>
    /// Сценарий §3.4 №2: НЛ за ≥ 70% И КПРФ против ≥ 50% (расхождение фракций)
    /// </summary>
    [SkippableFact]
    public async Task SearchScenario2NlAndKprfIntersect()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, nlPeriod) = await SeedFaction("NL", "Новые люди", convocation, "НЛ");
        var (_, kprfPeriod) = await SeedFaction("KPRF", "Коммунистическая партия Российской Федерации", convocation);
        var both = await seed.AddVote(convocation, "Оба условия", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(both, nlPeriod, 12, 0, 0, 3);
        await seed.AddStats(both, kprfPeriod, 2, 3, 1, 0);
        var onlyNl = await seed.AddVote(convocation, "Только НЛ", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(onlyNl, nlPeriod, 12, 0, 0, 3);
        await seed.AddStats(onlyNl, kprfPeriod, 5, 1, 0, 0);
        var onlyKprf = await seed.AddVote(convocation, "Только КПРФ", MoscowTime(2024, 5, 12, 12, 0, 3));
        await seed.AddStats(onlyKprf, nlPeriod, 2, 1, 0, 0);
        await seed.AddStats(onlyKprf, kprfPeriod, 2, 3, 1, 0);

        var result = await service.Search(new VoteSearchQueryModel
        {
            FactionConditions = [Condition("NL", "accept", 70m), Condition("KPRF", "decline", 50m)],
        }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(both.Id);
    }

    /// <summary>
    /// Сценарий §3.4 №3: законопроект 500091-8, третье чтение
    /// </summary>
    [SkippableFact]
    public async Task SearchScenario3BillThirdReading()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var bill = await seed.GetOrAddBill("500091-8");
        var otherBill = await seed.GetOrAddBill("123456-8");
        await seed.AddVote(convocation, "Первое чтение", MoscowTime(2024, 5, 10, 12, 0, 3), stage: VoteStage.FirstReading, bill: bill);
        await seed.AddVote(convocation, "Второе чтение", MoscowTime(2024, 5, 11, 12, 0, 3), stage: VoteStage.SecondReading, bill: bill);
        var third = await seed.AddVote(convocation, "Третье чтение", MoscowTime(2024, 5, 12, 12, 0, 3), stage: VoteStage.ThirdReading, bill: bill);
        await seed.AddVote(convocation, "Процедурное", MoscowTime(2024, 5, 13, 12, 0, 3), stage: VoteStage.Procedural);
        await seed.AddVote(convocation, "Чужой законопроект", MoscowTime(2024, 5, 14, 12, 0, 3), stage: VoteStage.ThirdReading, bill: otherBill);

        var result = await service.Search(new VoteSearchQueryModel { BillNumber = "500091-8", Stage = "thirdReading" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(third.Id);
        result.Items.Single().BillNumber.Should().Be("500091-8");
    }

    /// <summary>
    /// Сценарий §3.4 №4: «алкоголь», VIII созыв, ЕР за ≤ 99.9%
    /// </summary>
    [SkippableFact]
    public async Task SearchScenario4AlcoholConvocation8ErLte999()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var seventh = await seed.GetOrAddConvocation(7);
        var eighth = await seed.GetOrAddConvocation(8);
        var (_, er7Period) = await SeedFaction("ER", "Единая Россия", seventh);
        var (_, er8Period) = await SeedFaction("ER", "Единая Россия", eighth);
        var matching = await seed.AddVote(eighth, "О запрете продажи алкоголя в ночное время", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(matching, er8Period, 199, 0, 0, 1);
        var unanimous = await seed.AddVote(eighth, "Об ограничении продажи алкоголя", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(unanimous, er8Period, 200, 0, 0, 0);
        var oldConvocation = await seed.AddVote(seventh, "О запрете продажи алкоголя", MoscowTime(2020, 5, 10, 12, 0, 3));
        await seed.AddStats(oldConvocation, er7Period, 10, 10, 0, 0);
        var otherTopic = await seed.AddVote(eighth, "О налоге на добавленную стоимость", MoscowTime(2024, 5, 12, 12, 0, 3));
        await seed.AddStats(otherTopic, er8Period, 10, 10, 0, 0);

        var result = await service.Search(new VoteSearchQueryModel
        {
            Query = "алкоголь",
            Convocation = 8,
            FactionConditions = [Condition("ER", "accept", 99.9m, operatorToken: "lte")],
        }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(matching.Id);
    }

    /// <summary>
    /// Границы дат — московские сутки при историческом +04:00 (лето 2014) и +03:00:
    /// голосование 01.07.2014 00:15+04:00 хранится в UTC как 30.06 и попадает в фильтр 01.07
    /// </summary>
    [SkippableFact]
    public async Task SearchDateBoundsShouldUseHistoricalPlus0400()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(6);
        var juneLast = await seed.AddVote(convocation, "30 июня +04", MoscowTime(2014, 6, 30, 23, 30, 4), utcOffsetMinutes: 240);
        var julyFirst = await seed.AddVote(convocation, "1 июля +04", MoscowTime(2014, 7, 1, 0, 15, 4), utcOffsetMinutes: 240);
        var juneFirst = await seed.AddVote(convocation, "30 июня +03", MoscowTime(2014, 6, 30, 0, 10, 3), utcOffsetMinutes: 180);
        var julyFirstPlus03 = await seed.AddVote(convocation, "1 июля +03", MoscowTime(2014, 7, 1, 0, 5, 3), utcOffsetMinutes: 180);

        var july = await service.Search(new VoteSearchQueryModel { DateFrom = new DateOnly(2014, 7, 1), DateTo = new DateOnly(2014, 7, 1) }, CancellationToken.None);
        var june = await service.Search(new VoteSearchQueryModel { DateFrom = new DateOnly(2014, 6, 30), DateTo = new DateOnly(2014, 6, 30) }, CancellationToken.None);

        july.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { julyFirst.Id, julyFirstPlus03.Id });
        june.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { juneLast.Id, juneFirst.Id });
        july.Items.First(x => x.Id == julyFirst.Id).VotedAt.Offset.Should().Be(TimeSpan.FromHours(4));
    }

    /// <summary>
    /// EXISTS на PostgreSQL: NULL-процент не удовлетворяет условию даже с порогом 0;
    /// реальный 0.00% от состава проходит
    /// </summary>
    [SkippableFact]
    public async Task SearchExistsShouldExcludeNullPercent()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var emptyFaction = await seed.AddVote(convocation, "Пустой состав", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(emptyFaction, period, 0, 0, 0, 0);
        var nobodyVoted = await seed.AddVote(convocation, "Никто не голосовал", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(nobodyVoted, period, 0, 0, 0, 5);

        var members = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m)] }, CancellationToken.None);
        var voted = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m, basis: "voted")] }, CancellationToken.None);

        members.Items.Should().ContainSingle().Which.Id.Should().Be(nobodyVoted.Id);
        voted.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Пагинация на PostgreSQL: общее число считается до пагинации
    /// </summary>
    [SkippableFact]
    public async Task SearchPaginationShouldCountBeforePaging()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var votes = new List<Guid>();
        for (var day = 1; day <= 5; day++)
        {
            var vote = await seed.AddVote(convocation, $"Голосование {day}", MoscowTime(2024, 5, day, 12, 0, 3));
            votes.Add(vote.Id);
        }

        var second = await service.Search(new VoteSearchQueryModel { Page = 2, PageSize = 2 }, CancellationToken.None);
        var fourth = await service.Search(new VoteSearchQueryModel { Page = 4, PageSize = 2 }, CancellationToken.None);

        second.TotalCount.Should().Be(5);
        second.Items.Select(x => x.Id).Should().ContainInOrder(votes[2], votes[1]);
        fourth.Items.Should().BeEmpty();
        fourth.TotalCount.Should().Be(5);
    }

    /// <summary>
    /// П-3: три эталонных голосования — все счётчики и 7 процентов выдачи совпадают с ручным расчётом
    /// </summary>
    [SkippableFact]
    public async Task SearchReferenceVotesShouldMatchManualP3()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, erPeriod) = await SeedFaction("ER", "Единая Россия", convocation);
        var (_, nlPeriod) = await SeedFaction("NL", "Новые люди", convocation, "НЛ");
        var (_, kprfPeriod) = await SeedFaction("KPRF", "Коммунистическая партия Российской Федерации", convocation);
        var erVote = await seed.AddVote(convocation, "Эталон ЕР", MoscowTime(2024, 5, 10, 12, 0, 3), acceptCount: 289, declineCount: 30, abstainCount: 5);
        await seed.AddStats(erVote, erPeriod, 289, 30, 5, 0);
        var nlVote = await seed.AddVote(convocation, "Эталон НЛ", MoscowTime(2024, 5, 11, 12, 0, 3), acceptCount: 12, noneCount: 3);
        await seed.AddStats(nlVote, nlPeriod, 12, 0, 0, 3);
        var kprfVote = await seed.AddVote(convocation, "Эталон КПРФ", MoscowTime(2024, 5, 12, 12, 0, 3), acceptCount: 2, declineCount: 1, noneCount: 2);
        await seed.AddStats(kprfVote, kprfPeriod, 2, 1, 0, 2);

        var result = await service.Search(new VoteSearchQueryModel(), CancellationToken.None);

        result.TotalCount.Should().Be(3);
        var er = result.Items.Single(x => x.Id == erVote.Id).FactionStats.Single();
        er.MemberCount.Should().Be(324);
        er.VotedCount.Should().Be(324);
        er.AcceptCount.Should().Be(289);
        er.DeclineCount.Should().Be(30);
        er.AbstainCount.Should().Be(5);
        er.NoneCount.Should().Be(0);
        er.AcceptPctMembers.Should().Be(89.20m, "289/324 = 89.1975… → 89.20");
        er.DeclinePctMembers.Should().Be(9.26m, "30/324 = 9.2592… → 9.26");
        er.AbstainPctMembers.Should().Be(1.54m, "5/324 = 1.5432… → 1.54");
        er.NonePctMembers.Should().Be(0.00m);
        er.AcceptPctVoted.Should().Be(89.20m);
        er.DeclinePctVoted.Should().Be(9.26m);
        er.AbstainPctVoted.Should().Be(1.54m);
        var nl = result.Items.Single(x => x.Id == nlVote.Id).FactionStats.Single();
        nl.MemberCount.Should().Be(15);
        nl.VotedCount.Should().Be(12);
        nl.AcceptPctMembers.Should().Be(80.00m);
        nl.NonePctMembers.Should().Be(20.00m);
        nl.AcceptPctVoted.Should().Be(100.00m);
        nl.DeclinePctVoted.Should().Be(0.00m);
        nl.AbstainPctVoted.Should().Be(0.00m);
        var kprf = result.Items.Single(x => x.Id == kprfVote.Id).FactionStats.Single();
        kprf.MemberCount.Should().Be(5);
        kprf.VotedCount.Should().Be(3);
        kprf.AcceptPctMembers.Should().Be(40.00m);
        kprf.DeclinePctMembers.Should().Be(20.00m);
        kprf.AbstainPctMembers.Should().Be(0.00m);
        kprf.NonePctMembers.Should().Be(40.00m);
        kprf.AcceptPctVoted.Should().Be(66.67m, "2/3 = 66.666… → 66.67");
        kprf.DeclinePctVoted.Should().Be(33.33m, "1/3 = 33.333… → 33.33");
        kprf.AbstainPctVoted.Should().Be(0.00m);
    }

    /// <summary>
    /// Граница длины текстового запроса: 512 символов принимается, 513 — ошибка валидации
    /// </summary>
    [SkippableFact]
    public async Task SearchQueryLengthBoundaryShouldBeValidated()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "О налоге на добавленную стоимость", MoscowTime(2024, 5, 10, 12, 0, 3));

        var accepted = await service.Search(new VoteSearchQueryModel { Query = new string('а', 512) }, CancellationToken.None);
        var act = () => service.Search(new VoteSearchQueryModel { Query = new string('а', 513) }, CancellationToken.None);

        accepted.TotalCount.Should().Be(0);
        await act.Should().ThrowAsync<DumaLensValidationException>();
    }

    /// <summary>
    /// Создаёт фракцию и её период в созыве (историческое название периода — опционально)
    /// </summary>
    private async Task<(Entities.Faction Faction, Entities.FactionPeriod Period)> SeedFaction(string code, string title, Entities.Convocation convocation, string? periodTitle = null)
    {
        var faction = await seed.GetOrAddFaction(code, title);
        var period = await seed.GetOrAddPeriod(faction, convocation, periodTitle ?? title);
        return (faction, period);
    }

    /// <summary>
    /// Фракционное условие запроса с умолчаниями контракта (gte, members)
    /// </summary>
    private static FactionConditionQueryModel Condition(string faction, string result, decimal threshold, string operatorToken = "gte", string basis = "members")
        => new FactionConditionQueryModel
        {
            Faction = faction,
            Result = result,
            Operator = operatorToken,
            Threshold = threshold,
            Basis = basis,
        };

    /// <summary>
    /// Московское настенное время как мгновенное время UTC (конвенция хранения)
    /// </summary>
    private static DateTimeOffset MoscowTime(int year, int month, int day, int hour, int minute, int offsetHours)
        => new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.FromHours(offsetHours)).ToUniversalTime();
}