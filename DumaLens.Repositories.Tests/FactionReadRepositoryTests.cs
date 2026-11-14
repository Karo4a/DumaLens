using DumaLens.Context.Tests;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.ReadRepositories;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты <see cref="FactionReadRepository"/> на базе в памяти
/// </summary>
public class FactionReadRepositoryTests : DumaLensContextInMemory
{
    private readonly IFactionReadRepository repository;

    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionReadRepositoryTests()
    {
        repository = new FactionReadRepository(Context);
    }

    /// <summary>
    /// GetByCode должен возвращать фракцию по коду
    /// </summary>
    [Fact]
    public async Task GetByCodeShouldReturnFaction()
    {
        // Arrange
        var faction = await PrepareFaction();

        // Act
        var result = await repository.GetByCode(faction.Code, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(faction.Id);
        result.Code.Should().Be(faction.Code);
    }

    /// <summary>
    /// GetById должен возвращать null для мягко удалённой фракции
    /// </summary>
    [Fact]
    public async Task GetByIdShouldNotReturnDeletedFaction()
    {
        // Arrange
        var faction = await PrepareFaction(deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetById(faction.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetAll должен возвращать только неудалённые фракции, отсортированные по коду
    /// </summary>
    [Fact]
    public async Task GetAllShouldReturnActiveFactionsOrderedByCode()
    {
        // Arrange
        var first = await PrepareFaction();
        var second = await PrepareFaction();
        await PrepareFaction(deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetAll(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
        result.Should().BeInAscendingOrder(x => x.Code);
    }

    /// <summary>
    /// GetPeriodsByFaction должен возвращать периоды фракции, отсортированные по дате начала
    /// </summary>
    [Fact]
    public async Task GetPeriodsByFactionShouldReturnPeriodsOrderedByDateFrom()
    {
        // Arrange
        var period = await PrepareFactionPeriod();
        await PrepareFactionPeriod();

        // Act
        var result = await repository.GetPeriodsByFaction(period.FactionId, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result.Single().Id.Should().Be(period.Id);
        result.Single().SiteId.Should().Be(period.SiteId);
    }

    /// <summary>
    /// GetPeriodBySiteId должен находить период по сайту в рамках созыва
    /// </summary>
    [Fact]
    public async Task GetPeriodBySiteIdShouldReturnPeriodWithinConvocation()
    {
        // Arrange
        var period = await PrepareFactionPeriod();

        // Act
        var result = await repository.GetPeriodBySiteId(period.ConvocationId, period.SiteId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(period.Id);
        result.FactionId.Should().Be(period.FactionId);
    }

    /// <summary>
    /// GetPeriodBySiteId должен возвращать null для чужого созыва
    /// </summary>
    [Fact]
    public async Task GetPeriodBySiteIdShouldNotReturnPeriodOfOtherConvocation()
    {
        // Arrange
        var period = await PrepareFactionPeriod();
        var otherConvocation = await PrepareConvocation();

        // Act
        var result = await repository.GetPeriodBySiteId(otherConvocation.Id, period.SiteId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetPeriodBySiteId должен возвращать null для мягко удалённого периода
    /// </summary>
    [Fact]
    public async Task GetPeriodBySiteIdShouldNotReturnDeletedPeriod()
    {
        // Arrange
        var period = await PrepareFactionPeriod(deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetPeriodBySiteId(period.ConvocationId, period.SiteId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}