using Ahatornn.TestGenerator;
using DumaLens.Database.Tests.Infrastructure;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.Default;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace DumaLens.Database.Tests;

/// <summary>
/// Тесты ограничений колонок: длины, text без лимита, numeric(5,2), timestamptz, smallint
/// </summary>
/// <remarks>
/// Каждый тест выполняется в транзакции с откатом, чтобы не изменять общую базу коллекции.
/// </remarks>
[Collection("Database")]
public class ColumnConstraintTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ColumnConstraintTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// ФИО депутата длиннее лимита должно отклоняться базой данных
    /// </summary>
    [SkippableFact]
    public async Task DeputyFullNameShouldBeLimited()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var deputy = TestData.CreateDeputy(999101);
        deputy.FullName = new string('А', 257);
        context.Add(deputy);

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Номер законопроекта длиннее лимита должен отклоняться базой данных
    /// </summary>
    [SkippableFact]
    public async Task BillNumberShouldBeLimited()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(TestData.CreateBill(new string('9', 17)));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Заголовок голосования длиннее 512 символов должен сохраняться (text без лимита)
    /// </summary>
    [SkippableFact]
    public async Task VoteTitleShouldNotBeLimited()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var vote = TestData.CreateVote(ConvocationsDefault.DefaultConvocationToGuid(8), 999102);
        vote.Title = new string('О', 600);
        context.Add(vote);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var stored = await context.Set<Vote>().AsNoTracking().SingleAsync(x => x.Id == vote.Id);
        stored.Title.Should().HaveLength(600);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Проценты фракционной статистики должны округляться до двух знаков (numeric(5,2))
    /// </summary>
    [SkippableFact]
    public async Task PercentShouldRoundToTwoDecimals()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var vote = TestData.CreateVote(ConvocationsDefault.DefaultConvocationToGuid(8), 999103);
        context.Add(vote);
        await context.SaveChangesAsync();
        var period = FactionPeriodsDefault.Entities
            .First(x => x.ConvocationId == vote.ConvocationId);
        var stats = TestEntityProvider.Shared.Create<VoteFactionStats>(x =>
        {
            x.VoteId = vote.Id;
            x.FactionPeriodId = period.Id;
            x.FactionId = period.FactionId;
            x.AcceptCount = 10;
            x.DeclineCount = 5;
            x.AbstainCount = 2;
            x.NoneCount = 3;
            x.MemberCount = 20;
            x.VotedCount = 17;
            x.AcceptPctMembers = 12.345m;
            x.DeclinePctMembers = null;
            x.AbstainPctMembers = null;
            x.NonePctMembers = null;
            x.AcceptPctVoted = null;
            x.DeclinePctVoted = null;
            x.AbstainPctVoted = null;
            x.NonePctVoted = null;
            x.Vote = null!;
            x.FactionPeriod = null!;
            x.Faction = null!;
        });
        context.Add(stats);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var stored = await context.Set<VoteFactionStats>().AsNoTracking()
            .SingleAsync(x => x.VoteId == vote.Id && x.FactionPeriodId == period.Id);
        stored.AcceptPctMembers.Should().Be(12.35m);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Дата голосования должна хранить UTC-момент, а смещение источника — в отдельной колонке
    /// </summary>
    [SkippableFact]
    public async Task VotedAtShouldPreserveUtcInstantAndOffset()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var votedAt = new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(4));
        var vote = TestData.CreateVote(ConvocationsDefault.DefaultConvocationToGuid(8), 999104);
        vote.VotedAt = votedAt;
        vote.UtcOffsetMinutes = 240;
        context.Add(vote);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var stored = await context.Set<Vote>().AsNoTracking().SingleAsync(x => x.Id == vote.Id);
        stored.VotedAt.UtcDateTime.Should().Be(votedAt.UtcDateTime);
        stored.UtcOffsetMinutes.Should().Be(240);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Результат голоса депутата должен храниться как smallint и читаться без потерь
    /// </summary>
    [SkippableFact]
    public async Task DeputyVoteResultShouldRoundTripAsSmallint()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var vote = TestData.CreateVote(ConvocationsDefault.DefaultConvocationToGuid(8), 999105);
        var deputy = TestData.CreateDeputy(999106);
        context.AddRange(vote, deputy);
        await context.SaveChangesAsync();
        var deputyVote = TestEntityProvider.Shared.Create<DeputyVote>(x =>
        {
            x.VoteId = vote.Id;
            x.DeputyId = deputy.Id;
            x.FactionPeriodId = null;
            x.Result = VoteResult.Abstain;
            x.Vote = null!;
            x.Deputy = null!;
            x.FactionPeriod = null;
        });
        context.Add(deputyVote);

        // Act
        await context.SaveChangesAsync();

        // Assert
        var stored = await context.Set<DeputyVote>().AsNoTracking()
            .SingleAsync(x => x.VoteId == vote.Id && x.DeputyId == deputy.Id);
        stored.Result.Should().Be(VoteResult.Abstain);
        var columnType = await context.Database
            .SqlQueryRaw<string>("SELECT data_type AS \"Value\" FROM information_schema.columns WHERE table_name = 'DeputyVotes' AND column_name = 'Result'")
            .SingleAsync();
        columnType.Should().Be("smallint");
        await transaction.RollbackAsync();
    }
}