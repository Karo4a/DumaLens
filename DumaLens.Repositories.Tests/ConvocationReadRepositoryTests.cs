using DumaLens.Context.Tests;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.ReadRepositories;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты <see cref="ConvocationReadRepository"/> на базе в памяти
/// </summary>
public class ConvocationReadRepositoryTests : DumaLensContextInMemory
{
    private readonly IConvocationReadRepository repository;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ConvocationReadRepositoryTests()
    {
        repository = new ConvocationReadRepository(Context);
    }

    /// <summary>
    /// GetById должен возвращать созыв по идентификатору
    /// </summary>
    [Fact]
    public async Task GetByIdShouldReturnConvocation()
    {
        // Arrange
        var convocation = await PrepareConvocation();

        // Act
        var result = await repository.GetById(convocation.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(convocation.Id);
        result.Number.Should().Be(convocation.Number);
        result.SiteCode.Should().Be(convocation.SiteCode);
    }

    /// <summary>
    /// GetById должен возвращать null для мягко удалённого созыва
    /// </summary>
    [Fact]
    public async Task GetByIdShouldNotReturnDeletedConvocation()
    {
        // Arrange
        var convocation = await PrepareConvocation(deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetById(convocation.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetByNumber должен возвращать созыв по номеру
    /// </summary>
    [Fact]
    public async Task GetByNumberShouldReturnConvocation()
    {
        // Arrange
        var convocation = await PrepareConvocation();
        await PrepareConvocation();

        // Act
        var result = await repository.GetByNumber(convocation.Number, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(convocation.Id);
    }

    /// <summary>
    /// GetAll должен возвращать только неудалённые созывы, отсортированные по номеру
    /// </summary>
    [Fact]
    public async Task GetAllShouldReturnActiveConvocationsOrderedByNumber()
    {
        // Arrange
        var first = await PrepareConvocation();
        var second = await PrepareConvocation();
        await PrepareConvocation(deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetAll(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().BeEquivalentTo(new[] { first.Id, second.Id });
        result.Should().BeInAscendingOrder(x => x.Number);
    }
}