using DumaLens.Context.Tests;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.Contracts.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.ReadRepositories;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты <see cref="VoteReadRepository"/> на базе в памяти (уровень спецификации;
/// полнотекстовый поиск — только PostgreSQL, ADR-1)
/// </summary>
public class VoteReadRepositoryTests : DumaLensContextInMemory
{
    private readonly IVoteReadRepository repository;
    private readonly VoteSearchSeed seed;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteReadRepositoryTests()
    {
        repository = new VoteReadRepository(Context);
        seed = new VoteSearchSeed(Context);
    }

    /// <summary>
    /// Без условий и без фильтра hasResults пустые голосования входят в выдачу
    /// </summary>
    [Fact]
    public async Task SearchWithoutConditionsShouldIncludeEmptyVotes()
    {
        // Arrange
        var convocation = await seed.GetOrAddConvocation(8);
        var empty = await seed.AddVote(convocation, "Пустое", new DateTimeOffset(2024, 5, 10, 9, 0, 0, TimeSpan.Zero), hasResults: false);
        var full = await seed.AddVote(convocation, "Полное", new DateTimeOffset(2024, 5, 11, 9, 0, 0, TimeSpan.Zero));

        // Act
        var result = await repository.Search(Spec(), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Select(x => x.Id).Should().BeEquivalentTo(new[] { empty.Id, full.Id });
    }

    /// <summary>
    /// Мягко удалённые голосования не попадают в выдачу
    /// </summary>
    [Fact]
    public async Task SearchShouldExcludeSoftDeletedVotes()
    {
        // Arrange
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "Удалённое", new DateTimeOffset(2024, 5, 10, 9, 0, 0, TimeSpan.Zero), deletedAt: DateTimeOffset.UtcNow);
        var active = await seed.AddVote(convocation, "Активное", new DateTimeOffset(2024, 5, 11, 9, 0, 0, TimeSpan.Zero));

        // Act
        var result = await repository.Search(Spec(), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Single().Id.Should().Be(active.Id);
    }

    /// <summary>
    /// Комбинация none+voted запрещена контрактом и отклоняется репозиторием
    /// </summary>
    [Fact]
    public async Task SearchNoneVotedCombinationShouldThrow()
    {
        // Arrange
        var spec = Spec();
        spec.FactionConditions =
        [
            new VoteFactionConditionSpec
            {
                FactionId = Guid.NewGuid(),
                Result = VoteResult.None,
                Operator = ComparisonOperator.Gte,
                Threshold = 0m,
                Basis = PercentBasis.Voted,
            },
        ];

        // Act
        var act = () => repository.Search(spec, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Пагинация спецификации: страница за пределами первой и общее число до пагинации
    /// </summary>
    [Fact]
    public async Task SearchShouldApplySpecPagination()
    {
        // Arrange
        var convocation = await seed.GetOrAddConvocation(8);
        await seed.AddVote(convocation, "Первое", new DateTimeOffset(2024, 5, 10, 9, 0, 0, TimeSpan.Zero));
        var middle = await seed.AddVote(convocation, "Второе", new DateTimeOffset(2024, 5, 11, 9, 0, 0, TimeSpan.Zero));
        await seed.AddVote(convocation, "Третье", new DateTimeOffset(2024, 5, 12, 9, 0, 0, TimeSpan.Zero));
        var spec = Spec();
        spec.Page = 2;
        spec.PageSize = 1;

        // Act
        var result = await repository.Search(spec, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Single().Id.Should().Be(middle.Id);
    }

    /// <summary>
    /// Спецификация без фильтров с пагинацией по умолчанию
    /// </summary>
    private static VoteSearchSpec Spec()
        => new VoteSearchSpec { Page = 1, PageSize = 20 };
}