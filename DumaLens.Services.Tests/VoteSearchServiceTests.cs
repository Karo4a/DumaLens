using AutoMapper;
using DumaLens.Context.Tests;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Infrastructure;
using DumaLens.Services.Search;
using FluentAssertions;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Тесты поиска голосований (Э5, ФТ-20–ФТ-24) на базе в памяти: комбинации фильтров,
/// фракционные условия по И, граничные пороги, резолвинг фракций, пагинация, сортировки,
/// маппинг московского времени и валидация запроса. Полнотекстовый поиск — только PostgreSQL
/// (ADR-1), здесь текстовый фильтр не используется
/// </summary>
public sealed class VoteSearchServiceTests : DumaLensContextInMemory
{
    private readonly IVoteSearchService service;
    private readonly VoteSearchSeed seed;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteSearchServiceTests()
    {
        var configuration = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>());
        service = new VoteSearchService(
            new ValidateService(),
            new VoteReadRepository(Context),
            new ConvocationReadRepository(Context),
            new BillReadRepository(Context),
            new FactionReadRepository(Context),
            configuration.CreateMapper());
        seed = new VoteSearchSeed(Context);
    }

    /// <summary>
    /// Пустая база: пустая страница с эхом параметров пагинации
    /// </summary>
    [Fact]
    public async Task SearchEmptyDatabaseShouldReturnEmptyPage()
    {
        var result = await service.Search(new VoteSearchQueryModel(), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    /// <summary>
    /// Без фильтров: все голосования по убыванию даты, поля строки выдачи маппятся полностью
    /// </summary>
    [Fact]
    public async Task SearchWithoutFiltersShouldReturnAllByDateDesc()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var bill = await seed.GetOrAddBill("500091-8");
        var older = await seed.AddVote(convocation, "Первое", MoscowTime(2024, 5, 10, 12, 0, 3), acceptCount: 250);
        var middle = await seed.AddVote(convocation, "Второе", MoscowTime(2024, 5, 11, 12, 0, 3), stage: VoteStage.SecondReading, outcome: VoteOutcome.Rejected, acceptCount: 100, declineCount: 200, abstainCount: 5, noneCount: 125);
        var newer = await seed.AddVote(convocation, "Третье", MoscowTime(2024, 5, 12, 12, 0, 3), bill: bill, acceptCount: 300);

        var result = await service.Search(new VoteSearchQueryModel(), CancellationToken.None);

        result.TotalCount.Should().Be(3);
        result.Items.Select(x => x.Id).Should().ContainInOrder(newer.Id, middle.Id, older.Id);
        var item = result.Items.First(x => x.Id == middle.Id);
        item.SiteId.Should().Be(middle.SiteId);
        item.Title.Should().Be("Второе");
        item.Convocation.Should().Be(8);
        item.BillNumber.Should().BeNull();
        item.Stage.Should().Be(VoteStage.SecondReading);
        item.Outcome.Should().Be(VoteOutcome.Rejected);
        item.AcceptCount.Should().Be(100);
        item.DeclineCount.Should().Be(200);
        item.AbstainCount.Should().Be(5);
        item.NoneCount.Should().Be(125);
        item.HasResults.Should().BeTrue();
        result.Items.First(x => x.Id == newer.Id).BillNumber.Should().Be("500091-8");
    }

    /// <summary>
    /// При равной дате сортировка стабильна по идентификатору (устойчивая пагинация)
    /// </summary>
    [Fact]
    public async Task SearchEqualDatesShouldOrderById()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var votedAt = MoscowTime(2024, 5, 10, 12, 0, 3);
        var first = await seed.AddVote(convocation, "А", votedAt);
        var second = await seed.AddVote(convocation, "Б", votedAt);

        var result = await service.Search(new VoteSearchQueryModel(), CancellationToken.None);

        var expected = new[] { first.Id, second.Id }.Order();
        result.Items.Select(x => x.Id).Should().ContainInOrder(expected);
    }

    /// <summary>
    /// Фильтр по созыву возвращает только голосования этого созыва
    /// </summary>
    [Fact]
    public async Task SearchConvocationFilterShouldReturnOnlyThatConvocation()
    {
        var seventh = await seed.GetOrAddConvocation(7);
        var eighth = await seed.GetOrAddConvocation(8);
        await seed.AddVote(seventh, "Седьмой созыв", MoscowTime(2020, 5, 10, 12, 0, 3));
        var target = await seed.AddVote(eighth, "Восьмой созыв", MoscowTime(2024, 5, 10, 12, 0, 3));

        var result = await service.Search(new VoteSearchQueryModel { Convocation = 8 }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(target.Id);
    }

    /// <summary>
    /// Несуществующий (но допустимый) номер созыва — пустая выдача, не ошибка
    /// </summary>
    [Fact]
    public async Task SearchUnknownConvocationShouldReturnEmptyPage()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "Голосование", MoscowTime(2024, 5, 10, 12, 0, 3));

        var result = await service.Search(new VoteSearchQueryModel { Convocation = 9 }, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Фильтр по номеру законопроекта — точное совпадение; процедурные голосования не попадают
    /// </summary>
    [Fact]
    public async Task SearchBillNumberFilterShouldReturnOnlyBillVotes()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var bill = await seed.GetOrAddBill("500091-8");
        var otherBill = await seed.GetOrAddBill("123456-8");
        var target = await seed.AddVote(convocation, "По законопроекту", MoscowTime(2024, 5, 10, 12, 0, 3), bill: bill);
        await seed.AddVote(convocation, "По другому законопроекту", MoscowTime(2024, 5, 11, 12, 0, 3), bill: otherBill);
        await seed.AddVote(convocation, "Процедурное", MoscowTime(2024, 5, 12, 12, 0, 3), stage: VoteStage.Procedural);

        var result = await service.Search(new VoteSearchQueryModel { BillNumber = "500091-8" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(target.Id);
    }

    /// <summary>
    /// Неизвестный номер законопроекта — пустая выдача, не ошибка
    /// </summary>
    [Fact]
    public async Task SearchUnknownBillNumberShouldReturnEmptyPage()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "Голосование", MoscowTime(2024, 5, 10, 12, 0, 3));

        var result = await service.Search(new VoteSearchQueryModel { BillNumber = "999999-8" }, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Фильтр по стадии регистронезависим
    /// </summary>
    [Fact]
    public async Task SearchStageFilterShouldBeCaseInsensitive()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var target = await seed.AddVote(convocation, "Третье чтение", MoscowTime(2024, 5, 10, 12, 0, 3), stage: VoteStage.ThirdReading);
        await seed.AddVote(convocation, "Первое чтение", MoscowTime(2024, 5, 11, 12, 0, 3), stage: VoteStage.FirstReading);

        var result = await service.Search(new VoteSearchQueryModel { Stage = "thirdreading" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(target.Id);
    }

    /// <summary>
    /// Фильтр по исходу голосования
    /// </summary>
    [Fact]
    public async Task SearchOutcomeFilterShouldReturnMatchingOutcome()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "Принято", MoscowTime(2024, 5, 10, 12, 0, 3), outcome: VoteOutcome.Accepted);
        var rejected = await seed.AddVote(convocation, "Отклонено", MoscowTime(2024, 5, 11, 12, 0, 3), outcome: VoteOutcome.Rejected);

        var result = await service.Search(new VoteSearchQueryModel { Outcome = "rejected" }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(rejected.Id);
    }

    /// <summary>
    /// Границы дат — московские сутки, включительно, при обоих исторических смещениях (+03:00/+04:00):
    /// голосование 01.07.2014 00:15+04:00 хранится в UTC как 30.06 и обязано попасть в фильтр 01.07
    /// </summary>
    [Fact]
    public async Task SearchDateRangeShouldUseMoscowDaysWithHistoricalOffset()
    {
        var convocation = await seed.GetOrAddConvocation(6);
        var juneLast = await seed.AddVote(convocation, "30 июня +04", MoscowTime(2014, 6, 30, 23, 30, 4), utcOffsetMinutes: 240);
        var julyFirst = await seed.AddVote(convocation, "1 июля +04", MoscowTime(2014, 7, 1, 0, 15, 4), utcOffsetMinutes: 240);
        var juneFirst = await seed.AddVote(convocation, "30 июня +03", MoscowTime(2014, 6, 30, 0, 10, 3), utcOffsetMinutes: 180);
        var julyFirstPlus03 = await seed.AddVote(convocation, "1 июля +03", MoscowTime(2014, 7, 1, 0, 5, 3), utcOffsetMinutes: 180);

        var july = await service.Search(new VoteSearchQueryModel { DateFrom = new DateOnly(2014, 7, 1), DateTo = new DateOnly(2014, 7, 1) }, CancellationToken.None);
        var june = await service.Search(new VoteSearchQueryModel { DateFrom = new DateOnly(2014, 6, 30), DateTo = new DateOnly(2014, 6, 30) }, CancellationToken.None);

        july.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { julyFirst.Id, julyFirstPlus03.Id });
        june.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { juneLast.Id, juneFirst.Id });
    }

    /// <summary>
    /// Фракционные условия по умолчанию включают только голосования с результатами;
    /// явный hasResults=false переопределяет умолчание
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionsShouldDefaultToHasResults()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var empty = await seed.AddVote(convocation, "Пустое", MoscowTime(2024, 5, 10, 12, 0, 3), hasResults: false);
        await seed.AddStats(empty, period, 10, 0, 0, 0);
        var query = new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 50m)] };

        var defaultResult = await service.Search(query, CancellationToken.None);
        query.HasResults = false;
        var explicitResult = await service.Search(query, CancellationToken.None);

        defaultResult.Items.Should().BeEmpty("умолчание при фракционных условиях — hasResults=true");
        explicitResult.Items.Should().ContainSingle().Which.Id.Should().Be(empty.Id);
    }

    /// <summary>
    /// Пагинация: страница, размер и общее число до пагинации; страница за пределами — пустая
    /// </summary>
    [Fact]
    public async Task SearchPaginationShouldReturnRequestedPageAndTotalCount()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var votes = new List<Guid>();
        for (var day = 1; day <= 5; day++)
        {
            var vote = await seed.AddVote(convocation, $"Голосование {day}", MoscowTime(2024, 5, day, 12, 0, 3));
            votes.Add(vote.Id);
        }

        var second = await service.Search(new VoteSearchQueryModel { Page = 2, PageSize = 2 }, CancellationToken.None);
        var third = await service.Search(new VoteSearchQueryModel { Page = 3, PageSize = 2 }, CancellationToken.None);
        var fourth = await service.Search(new VoteSearchQueryModel { Page = 4, PageSize = 2 }, CancellationToken.None);

        second.TotalCount.Should().Be(5);
        second.Items.Select(x => x.Id).Should().ContainInOrder(votes[2], votes[1]);
        third.Items.Select(x => x.Id).Should().ContainInOrder(votes[0]);
        fourth.Items.Should().BeEmpty();
        fourth.TotalCount.Should().Be(5);
    }

    /// <summary>
    /// Сортировка по числу голосов «за» по убыванию
    /// </summary>
    [Fact]
    public async Task SearchSortAcceptCountDescShouldOrderByAcceptCount()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var low = await seed.AddVote(convocation, "Мало", MoscowTime(2024, 5, 10, 12, 0, 3), acceptCount: 10);
        var high = await seed.AddVote(convocation, "Много", MoscowTime(2024, 5, 11, 12, 0, 3), acceptCount: 30);
        var middle = await seed.AddVote(convocation, "Средне", MoscowTime(2024, 5, 12, 12, 0, 3), acceptCount: 20);

        var result = await service.Search(new VoteSearchQueryModel { Sort = "acceptCountDesc" }, CancellationToken.None);

        result.Items.Select(x => x.Id).Should().ContainInOrder(high.Id, middle.Id, low.Id);
    }

    /// <summary>
    /// Сортировка по дате по возрастанию
    /// </summary>
    [Fact]
    public async Task SearchSortDateAscShouldOrderByDateAscending()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var older = await seed.AddVote(convocation, "Раньше", MoscowTime(2024, 5, 10, 12, 0, 3));
        var newer = await seed.AddVote(convocation, "Позже", MoscowTime(2024, 5, 11, 12, 0, 3));

        var result = await service.Search(new VoteSearchQueryModel { Sort = "dateAsc" }, CancellationToken.None);

        result.Items.Select(x => x.Id).Should().ContainInOrder(older.Id, newer.Id);
    }

    /// <summary>
    /// Порог «за» ≥ 70% от состава: 80% попадает, 66.67% — нет (сценарий §3.4 №1 на мини-данных)
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionAcceptGte70ShouldFilterByMembersPercent()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("NL", "Новые люди", convocation, "НЛ");
        var matching = await seed.AddVote(convocation, "Подходит", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(matching, period, 12, 0, 0, 3);
        var notMatching = await seed.AddVote(convocation, "Не подходит", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(notMatching, period, 2, 1, 0, 0);

        var result = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("NL", "accept", 70m)] }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(matching.Id);
    }

    /// <summary>
    /// Граничные пороги 0 и 100: ≥0 включает нулевой процент, ≥100 — только единогласные, ≤0 — только нулевые
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionBoundaryThresholdsShouldBeInclusive()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var zero = await seed.AddVote(convocation, "Ноль за", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(zero, period, 0, 10, 0, 0);
        var hundred = await seed.AddVote(convocation, "Сто за", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(hundred, period, 10, 0, 0, 0);

        var gteZero = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m)] }, CancellationToken.None);
        var gteHundred = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 100m)] }, CancellationToken.None);
        var lteZero = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m, operatorToken: "lte")] }, CancellationToken.None);
        var lteHundred = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 100m, operatorToken: "lte")] }, CancellationToken.None);

        gteZero.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { zero.Id, hundred.Id });
        gteHundred.Items.Should().ContainSingle().Which.Id.Should().Be(hundred.Id);
        lteZero.Items.Should().ContainSingle().Which.Id.Should().Be(zero.Id);
        lteHundred.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { zero.Id, hundred.Id });
    }

    /// <summary>
    /// NULL-процент не удовлетворяет условию даже с порогом 0: пустой состав (NULL от состава),
    /// никто не голосовал (NULL от голосовавших). Реальный 0.00% от состава порог ≥0 проходит
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionNullPercentShouldNeverMatch()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var emptyFaction = await seed.AddVote(convocation, "Пустой состав", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(emptyFaction, period, 0, 0, 0, 0);
        var nobodyVoted = await seed.AddVote(convocation, "Никто не голосовал", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(nobodyVoted, period, 0, 0, 0, 5);

        var members = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m)] }, CancellationToken.None);
        var voted = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m, basis: "voted")] }, CancellationToken.None);

        members.Items.Should().ContainSingle().Which.Id.Should().Be(nobodyVoted.Id, "0.00% от состава — реальное значение, порог ≥0 его включает");
        voted.Items.Should().BeEmpty("у обоих голосований процент от голосовавших — NULL");
    }

    /// <summary>
    /// База «от проголосовавших»: 12 из 15 состава, но все 12 голосовавших — за (80% состава, 100% голосовавших)
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionVotedBasisShouldUseVotedPercent()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("NL", "Новые люди", convocation);
        var vote = await seed.AddVote(convocation, "Голосование", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(vote, period, 12, 0, 0, 3);

        var byVoted = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("NL", "accept", 90m, basis: "voted")] }, CancellationToken.None);
        var byMembers = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("NL", "accept", 90m)] }, CancellationToken.None);

        byVoted.Items.Should().ContainSingle().Which.Id.Should().Be(vote.Id);
        byMembers.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Результат «не голосовал» считается только от состава: 3 из 15 = 20%
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionNoneShouldUseMembersPercent()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var vote = await seed.AddVote(convocation, "Бойкот", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(vote, period, 12, 0, 0, 3);

        var matching = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "none", 20m)] }, CancellationToken.None);
        var notMatching = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("ER", "none", 20.01m)] }, CancellationToken.None);

        matching.Items.Should().ContainSingle().Which.Id.Should().Be(vote.Id);
        notMatching.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Результаты «против» и «воздержался» с операторами ≥ и ≤
    /// </summary>
    [Fact]
    public async Task SearchFactionConditionDeclineAndAbstainShouldFilter()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("KPRF", "Коммунистическая партия Российской Федерации", convocation);
        var protest = await seed.AddVote(convocation, "Протест", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(protest, period, 2, 3, 1, 0);
        var loyal = await seed.AddVote(convocation, "Лояльное", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(loyal, period, 18, 0, 1, 1);

        var decline = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("KPRF", "decline", 50m)] }, CancellationToken.None);
        var abstain = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("KPRF", "abstain", 10m, operatorToken: "lte")] }, CancellationToken.None);

        decline.Items.Should().ContainSingle().Which.Id.Should().Be(protest.Id);
        abstain.Items.Should().ContainSingle().Which.Id.Should().Be(loyal.Id);
    }

    /// <summary>
    /// Несколько условий объединяются по И: разные фракции должны одновременно удовлетворять
    /// своим порогам (сценарий §3.4 №2 на мини-данных)
    /// </summary>
    [Fact]
    public async Task SearchTwoFactionConditionsShouldIntersect()
    {
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
        var neither = await seed.AddVote(convocation, "Ни одного", MoscowTime(2024, 5, 13, 12, 0, 3));
        await seed.AddStats(neither, nlPeriod, 2, 1, 0, 0);
        await seed.AddStats(neither, kprfPeriod, 5, 1, 0, 0);

        var result = await service.Search(new VoteSearchQueryModel
        {
            FactionConditions = [Condition("NL", "accept", 70m), Condition("KPRF", "decline", 50m)],
        }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(both.Id);
    }

    /// <summary>
    /// Два условия по одной фракции (разные результаты) также объединяются по И
    /// </summary>
    [Fact]
    public async Task SearchSameFactionTwoConditionsShouldIntersect()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var matching = await seed.AddVote(convocation, "Подходит", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(matching, period, 9, 1, 0, 5);
        var notMatching = await seed.AddVote(convocation, "Не подходит", MoscowTime(2024, 5, 11, 12, 0, 3));
        await seed.AddStats(notMatching, period, 7, 3, 0, 5);

        var result = await service.Search(new VoteSearchQueryModel
        {
            FactionConditions = [Condition("ER", "accept", 60m), Condition("ER", "decline", 10m, operatorToken: "lte")],
        }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(matching.Id);
    }

    /// <summary>
    /// Фракция резолвится по коду, каноническому и историческому названию, регистронезависимо
    /// </summary>
    [Theory]
    [InlineData("NL")]
    [InlineData("nl")]
    [InlineData("Новые люди")]
    [InlineData("НЛ")]
    public async Task SearchFactionTokenShouldResolveCodeTitleAndHistoricalName(string token)
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("NL", "Новые люди", convocation, "НЛ");
        var vote = await seed.AddVote(convocation, "Голосование", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(vote, period, 12, 0, 0, 3);

        var result = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition(token, "accept", 70m)] }, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(vote.Id);
    }

    /// <summary>
    /// Историческое название «СР-Р» резолвится в каноническую фракцию SR
    /// </summary>
    [Fact]
    public async Task SearchHistoricalNameSrRShouldResolveToCanonicalFaction()
    {
        var convocation = await seed.GetOrAddConvocation(7);
        var (faction, period) = await SeedFaction("SR", "Справедливая Россия", convocation, "СР-Р");
        var vote = await seed.AddVote(convocation, "Голосование", MoscowTime(2020, 5, 10, 12, 0, 3));
        await seed.AddStats(vote, period, 12, 0, 0, 3);

        var result = await service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("СР-Р", "accept", 70m)] }, CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Id.Should().Be(vote.Id);
        result.Items.Single().FactionStats.Single().FactionId.Should().Be(faction.Id);
    }

    /// <summary>
    /// Неизвестная фракция — ошибка валидации с полем Faction
    /// </summary>
    [Fact]
    public async Task SearchUnknownFactionShouldThrowValidationException()
    {
        var act = () => service.Search(new VoteSearchQueryModel { FactionConditions = [Condition("XXX", "accept", 50m)] }, CancellationToken.None);

        var exception = (await act.Should().ThrowAsync<DumaLensValidationException>()).Which;
        exception.Errors.Should().ContainSingle().Which.Field.Should().Be("Faction");
    }

    /// <summary>
    /// Фракционные строки выдачи: все счётчики и 7 процентов, порядок по коду фракции
    /// </summary>
    [Fact]
    public async Task SearchShouldAttachFactionStatsWithAllCountersAndPercents()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, erPeriod) = await SeedFaction("ER", "Единая Россия", convocation);
        var (_, kprfPeriod) = await SeedFaction("KPRF", "Коммунистическая партия Российской Федерации", convocation);
        var vote = await seed.AddVote(convocation, "Голосование", MoscowTime(2024, 5, 10, 12, 0, 3));
        await seed.AddStats(vote, erPeriod, 12, 2, 1, 0);
        await seed.AddStats(vote, kprfPeriod, 0, 10, 0, 5);

        var result = await service.Search(new VoteSearchQueryModel(), CancellationToken.None);

        var stats = result.Items.Single().FactionStats;
        stats.Select(x => x.FactionCode).Should().ContainInOrder("ER", "KPRF");
        var er = stats.First(x => x.FactionCode == "ER");
        er.FactionTitle.Should().Be("Единая Россия");
        er.MemberCount.Should().Be(15);
        er.VotedCount.Should().Be(15);
        er.AcceptCount.Should().Be(12);
        er.DeclineCount.Should().Be(2);
        er.AbstainCount.Should().Be(1);
        er.NoneCount.Should().Be(0);
        er.AcceptPctMembers.Should().Be(80.00m);
        er.DeclinePctMembers.Should().Be(13.33m);
        er.AbstainPctMembers.Should().Be(6.67m);
        er.NonePctMembers.Should().Be(0.00m);
        er.AcceptPctVoted.Should().Be(80.00m);
        er.DeclinePctVoted.Should().Be(13.33m);
        er.AbstainPctVoted.Should().Be(6.67m);
        var kprf = stats.First(x => x.FactionCode == "KPRF");
        kprf.MemberCount.Should().Be(15);
        kprf.VotedCount.Should().Be(10);
        kprf.DeclinePctMembers.Should().Be(66.67m);
        kprf.NonePctMembers.Should().Be(33.33m);
        kprf.AcceptPctVoted.Should().Be(0.00m);
        kprf.DeclinePctVoted.Should().Be(100.00m);
        kprf.AbstainPctVoted.Should().Be(0.00m);
    }

    /// <summary>
    /// Дата голосования в выдаче — с историческим московским смещением (+04:00 лето 2014)
    /// </summary>
    [Fact]
    public async Task SearchShouldMapVotedAtWithHistoricalOffset()
    {
        var convocation = await seed.GetOrAddConvocation(6);
        await seed.AddVote(convocation, "Лето 2014", MoscowTime(2014, 7, 1, 14, 30, 4), utcOffsetMinutes: 240);

        var result = await service.Search(new VoteSearchQueryModel(), CancellationToken.None);

        var votedAt = result.Items.Single().VotedAt;
        votedAt.Offset.Should().Be(TimeSpan.FromHours(4));
        votedAt.DateTime.Should().Be(new DateTime(2014, 7, 1, 14, 30, 0));
        votedAt.UtcDateTime.Should().Be(new DateTime(2014, 7, 1, 10, 30, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Невалидные запросы отклоняются ошибкой валидации (422 на уровне API)
    /// </summary>
    [Fact]
    public async Task SearchInvalidRequestsShouldThrowValidationException()
    {
        var cases = new (string Name, Action<VoteSearchQueryModel> Mutate)[]
        {
            ("page 0", query => query.Page = 0),
            ("pageSize 0", query => query.PageSize = 0),
            ("pageSize 101", query => query.PageSize = 101),
            ("созыв 0", query => query.Convocation = 0),
            ("созыв 10", query => query.Convocation = 10),
            ("dateFrom позже dateTo", query => { query.DateFrom = new DateOnly(2024, 5, 2); query.DateTo = new DateOnly(2024, 5, 1); }),
            ("запрос 513 символов", query => query.Query = new string('а', 513)),
            ("неизвестная стадия", query => query.Stage = "FourthReading"),
            ("числовая стадия", query => query.Stage = "4"),
            ("неизвестный исход", query => query.Outcome = "passed"),
            ("неизвестная сортировка", query => query.Sort = "votes"),
            ("relevance без запроса", query => query.Sort = "relevance"),
            ("11 фракционных условий", query => query.FactionConditions = Enumerable.Range(0, 11).Select(_ => Condition("ER", "accept", 50m)).ToList()),
            ("порог -0.01", query => query.FactionConditions = [Condition("ER", "accept", -0.01m)]),
            ("порог 100.01", query => query.FactionConditions = [Condition("ER", "accept", 100.01m)]),
            ("неизвестный результат фракции", query => query.FactionConditions = [Condition("ER", "for", 50m)]),
            ("неизвестный оператор", query => query.FactionConditions = [Condition("ER", "accept", 50m, operatorToken: "gt")]),
            ("неизвестная база процента", query => query.FactionConditions = [Condition("ER", "accept", 50m, basis: "present")]),
            ("none+voted запрещён", query => query.FactionConditions = [Condition("ER", "none", 50m, basis: "voted")]),
            ("пустой токен фракции", query => query.FactionConditions = [Condition("", "accept", 50m)]),
        };
        foreach (var (name, mutate) in cases)
        {
            var query = new VoteSearchQueryModel();
            mutate(query);
            var act = () => service.Search(query, CancellationToken.None);
            await act.Should().ThrowAsync<DumaLensValidationException>($"кейс «{name}» должен отклоняться валидацией");
        }
    }

    /// <summary>
    /// Граничные допустимые значения принимаются: размеры страницы 1/100, пороги 0/100,
    /// 10 условий, созывы 1/9, dateFrom=dateTo
    /// </summary>
    [Fact]
    public async Task SearchBoundaryValuesShouldBeAccepted()
    {
        var convocation = await seed.GetOrAddConvocation(8);
        var (_, period) = await SeedFaction("ER", "Единая Россия", convocation);
        var vote = await seed.AddVote(convocation, "Границы", MoscowTime(2024, 5, 15, 12, 0, 3), acceptCount: 10);
        await seed.AddStats(vote, period, 10, 0, 0, 0);
        var queries = new[]
        {
            new VoteSearchQueryModel { PageSize = 1 },
            new VoteSearchQueryModel { PageSize = 100 },
            new VoteSearchQueryModel { Convocation = 1 },
            new VoteSearchQueryModel { Convocation = 9 },
            new VoteSearchQueryModel { DateFrom = new DateOnly(2024, 5, 15), DateTo = new DateOnly(2024, 5, 15) },
            new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 0m)] },
            new VoteSearchQueryModel { FactionConditions = [Condition("ER", "accept", 100m)] },
            new VoteSearchQueryModel { FactionConditions = Enumerable.Range(0, 10).Select(_ => Condition("ER", "accept", 50m)).ToList() },
        };
        foreach (var query in queries)
        {
            var result = await service.Search(query, CancellationToken.None);
            result.Should().NotBeNull();
        }
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