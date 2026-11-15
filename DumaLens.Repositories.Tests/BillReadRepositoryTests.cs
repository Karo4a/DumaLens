using DumaLens.Context.Tests;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.ReadRepositories;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты <see cref="BillReadRepository"/> на базе в памяти
/// </summary>
public class BillReadRepositoryTests : DumaLensContextInMemory
{
    private readonly IBillReadRepository repository;
    private readonly VoteSearchSeed seed;

    /// <summary>
    /// Конструктор
    /// </summary>
    public BillReadRepositoryTests()
    {
        repository = new BillReadRepository(Context);
        seed = new VoteSearchSeed(Context);
    }

    /// <summary>
    /// GetByNumber должен возвращать законопроект по точному номеру
    /// </summary>
    [Fact]
    public async Task GetByNumberShouldReturnBill()
    {
        // Arrange
        var bill = await seed.GetOrAddBill("500091-8");

        // Act
        var result = await repository.GetByNumber("500091-8", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(bill.Id);
        result.Number.Should().Be("500091-8");
    }

    /// <summary>
    /// GetByNumber должен возвращать null для неизвестного номера
    /// </summary>
    [Fact]
    public async Task GetByNumberShouldReturnNullForUnknownNumber()
    {
        // Arrange
        await seed.GetOrAddBill("500091-8");

        // Act
        var result = await repository.GetByNumber("999999-8", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetByNumber должен возвращать null для мягко удалённого законопроекта
    /// </summary>
    [Fact]
    public async Task GetByNumberShouldNotReturnDeletedBill()
    {
        // Arrange
        await seed.GetOrAddBill("111111-8", deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetByNumber("111111-8", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}