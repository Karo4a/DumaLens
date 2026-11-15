using Ahatornn.TestGenerator;
using AutoMapper;
using DumaLens.Context.Tests;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.ReadRepositories;
using DumaLens.Services.Cards;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Тесты карточки голосования (Э6): группировка голосов депутатов по канонической фракции,
/// детерминированный заголовок группы при нескольких периодах одной фракции в созыве,
/// порядок групп и ошибка отсутствия голосования
/// </summary>
public sealed class VoteCardServiceTests : DumaLensContextInMemory
{
    private readonly IVoteCardService service;
    private readonly VoteSearchSeed seed;
    private long nextPeriodSiteId = 900_000_000;
    private long nextDeputySiteId = 910_000_000;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteCardServiceTests()
    {
        var configuration = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>());
        service = new VoteCardService(new VoteReadRepository(Context), configuration.CreateMapper());
        seed = new VoteSearchSeed(Context);
    }

    /// <summary>
    /// Несколько периодов одной фракции в созыве: одна группа по каноническому коду,
    /// заголовок — название периода, под которым голосовало большинство депутатов
    /// </summary>
    [Fact]
    public async Task GetDetails_MultiplePeriodsOfOneFaction_GroupTitleByMajority()
    {
        // Arrange
        var convocation = await seed.GetOrAddConvocation(3);
        var faction = await seed.GetOrAddFaction("ER", "Единая Россия");
        var minorityPeriod = await AddPeriod(faction, convocation, "О-ЕР");
        var majorityPeriod = await AddPeriod(faction, convocation, "Е-ЕР");
        var vote = await seed.AddVote(convocation, "О проекте федерального закона", Moscow(2003, 6, 1));
        await AddDeputyVote(vote, minorityPeriod, "Аaaa", VoteResult.Decline);
        await AddDeputyVote(vote, majorityPeriod, "Бbbb", VoteResult.Accept);
        await AddDeputyVote(vote, majorityPeriod, "Вvvv", VoteResult.Accept);

        // Act
        var details = await service.GetDetails(vote.SiteId, CancellationToken.None);

        // Assert
        var group = details.DeputyVoteGroups.Should().ContainSingle().Subject;
        group.FactionCode.Should().Be("ER");
        group.FactionTitle.Should().Be("Е-ЕР", "заголовок группы — название периода, под которым голосовало большинство");
        group.Deputies.Should().HaveCount(3);
    }

    /// <summary>
    /// При равенстве голосов между периодами заголовок выбирается ordinal-сравнением названий
    /// </summary>
    [Fact]
    public async Task GetDetails_EqualPeriods_GroupTitleByOrdinal()
    {
        // Arrange
        var convocation = await seed.GetOrAddConvocation(3);
        var faction = await seed.GetOrAddFaction("ER", "Единая Россия");
        var periodA = await AddPeriod(faction, convocation, "Е-ЕР");
        var periodB = await AddPeriod(faction, convocation, "О-ЕР");
        var vote = await seed.AddVote(convocation, "О проекте федерального закона", Moscow(2003, 6, 1));
        await AddDeputyVote(vote, periodB, "Аaaa", VoteResult.Decline);
        await AddDeputyVote(vote, periodA, "Бbbb", VoteResult.Accept);

        // Act
        var details = await service.GetDetails(vote.SiteId, CancellationToken.None);

        // Assert
        var group = details.DeputyVoteGroups.Should().ContainSingle().Subject;
        group.FactionTitle.Should().Be("Е-ЕР", "при равенстве голосов название выбирается ordinal-сравнением, а не порядком депутатов");
    }

    /// <summary>
    /// Депутаты без фракции образуют отдельную группу, которая идёт после фракционных
    /// </summary>
    [Fact]
    public async Task GetDetails_DeputyWithoutFaction_GroupLast()
    {
        // Arrange
        var convocation = await seed.GetOrAddConvocation(8);
        var faction = await seed.GetOrAddFaction("KPRF", "КПРФ");
        var period = await AddPeriod(faction, convocation, "КПРФ");
        var vote = await seed.AddVote(convocation, "Процедурное голосование", Moscow(2024, 3, 13));
        await AddDeputyVote(vote, null, "Аaaa", VoteResult.None);
        await AddDeputyVote(vote, period, "Бbbb", VoteResult.Accept);

        // Act
        var details = await service.GetDetails(vote.SiteId, CancellationToken.None);

        // Assert
        var groups = details.DeputyVoteGroups.ToArray();
        groups.Should().HaveCount(2);
        groups[0].FactionCode.Should().Be("KPRF");
        groups[1].FactionCode.Should().BeNull();
        groups[1].FactionTitle.Should().BeNull();
        groups[1].Deputies.Should().ContainSingle();
    }

    /// <summary>
    /// Несуществующее голосование — доменное исключение отсутствия
    /// </summary>
    [Fact]
    public async Task GetDetails_UnknownVote_ThrowsNotFound()
    {
        // Act
        var act = async () => await service.GetDetails(999_999_999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DumaLensNotFoundException>();
    }

    /// <summary>
    /// Создаёт новый период фракции в созыве (сеяльщик GetOrAddPeriod переименовывает существующий,
    /// а тесту нужны два одновременных периода одной фракции)
    /// </summary>
    private async Task<FactionPeriod> AddPeriod(Faction faction, Convocation convocation, string title)
    {
        var period = new FactionPeriod
        {
            FactionId = faction.Id,
            ConvocationId = convocation.Id,
            SiteId = nextPeriodSiteId++,
            Title = title,
            DateFrom = convocation.DateFrom,
            DateTo = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = null,
        };
        Context.Add(period);
        await UnitOfWork.SaveChangesAsync();
        return period;
    }

    /// <summary>
    /// Добавляет депутата и его голос в голосовании (null-период — депутат без фракции)
    /// </summary>
    private async Task AddDeputyVote(Vote vote, FactionPeriod? period, string sortName, VoteResult result)
    {
        var deputy = TestEntityProvider.Shared.Create<Deputy>(x =>
        {
            x.SiteId = nextDeputySiteId++;
            x.FullName = sortName;
            x.SortName = sortName;
            x.CreatedAt = DateTimeOffset.UtcNow;
            x.UpdatedAt = DateTimeOffset.UtcNow;
            x.DeletedAt = null;
        });
        Context.Add(deputy);
        Context.Add(new DeputyVote
        {
            VoteId = vote.Id,
            DeputyId = deputy.Id,
            FactionPeriodId = period?.Id,
            Result = result,
        });
        await UnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Московское время голосования (смещение +03:00)
    /// </summary>
    private static DateTimeOffset Moscow(int year, int month, int day)
        => new(year, month, day, 12, 0, 0, TimeSpan.FromHours(3));
}