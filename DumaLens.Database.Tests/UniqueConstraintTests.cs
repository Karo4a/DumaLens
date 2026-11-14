using Ahatornn.TestGenerator;
using DumaLens.Database.Tests.Infrastructure;
using DumaLens.Entities;
using DumaLens.Entities.Default;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace DumaLens.Database.Tests;

/// <summary>
/// Тесты уникальных ограничений справочников и auth-таблиц
/// </summary>
/// <remarks>
/// Каждый тест выполняется в транзакции с откатом, чтобы не изменять общую базу коллекции.
/// </remarks>
[Collection("Database")]
public class UniqueConstraintTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UniqueConstraintTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Номер созыва должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task ConvocationNumberShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(TestData.CreateConvocation(8, "DUPLICATE1"));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Код канонической фракции должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task FactionCodeShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(TestData.CreateFaction("ER"));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Идентификатор сайта фракции должен быть уникальным в рамках созыва
    /// </summary>
    [SkippableFact]
    public async Task FactionPeriodSiteIdShouldBeUniquePerConvocation()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var existing = FactionPeriodsDefault.Entities
            .First(x => x.ConvocationId == ConvocationsDefault.DefaultConvocationToGuid(8));
        var duplicate = TestEntityProvider.Shared.Create<FactionPeriod>(x =>
        {
            x.CreatedAt = SeedTimestamp.Value;
            x.UpdatedAt = SeedTimestamp.Value;
            x.DeletedAt = null;
            x.FactionId = existing.FactionId;
            x.ConvocationId = existing.ConvocationId;
            x.SiteId = existing.SiteId;
            x.Title = "Дубликат периода";
            x.DateFrom = existing.DateFrom;
            x.DateTo = null;
            x.Faction = null!;
            x.Convocation = null!;
        });
        context.Add(duplicate);

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Идентификатор сайта депутата должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task DeputySiteIdShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(TestData.CreateDeputy(999001));
        await context.SaveChangesAsync();
        context.Add(TestData.CreateDeputy(999001));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Номер законопроекта должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task BillNumberShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(TestData.CreateBill("123456-8"));
        await context.SaveChangesAsync();
        context.Add(TestData.CreateBill("123456-8"));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Идентификатор сайта голосования должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task VoteSiteIdShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var convocationId = ConvocationsDefault.DefaultConvocationToGuid(8);
        context.Add(TestData.CreateVote(convocationId, 999001));
        await context.SaveChangesAsync();
        context.Add(TestData.CreateVote(convocationId, 999001));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Логин в нижнем регистре должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task UserLoginLowerCaseShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(TestData.CreateUser("TestUser1", "user1@example.com"));
        await context.SaveChangesAsync();
        var duplicate = TestData.CreateUser("TestUser2", "user2@example.com");
        duplicate.LoginLowerCase = "testuser1";
        context.Add(duplicate);

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Хэш refresh-токена должен быть уникальным
    /// </summary>
    [SkippableFact]
    public async Task RefreshTokenHashShouldBeUnique()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var user = TestData.CreateUser("TokenUser1", "token1@example.com");
        context.Add(user);
        await context.SaveChangesAsync();
        var hash = TestData.HashOf("refresh-token-1");
        context.Add(TestData.CreateRefreshToken(user.Id, hash));
        await context.SaveChangesAsync();
        context.Add(TestData.CreateRefreshToken(user.Id, hash));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// У пользователя должен быть только один активный refresh-токен
    /// </summary>
    [SkippableFact]
    public async Task ActiveRefreshTokenShouldBeUniquePerUser()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var user = TestData.CreateUser("TokenUser2", "token2@example.com");
        context.Add(user);
        await context.SaveChangesAsync();
        context.Add(TestData.CreateRefreshToken(user.Id, TestData.HashOf("refresh-token-a")));
        await context.SaveChangesAsync();
        context.Add(TestData.CreateRefreshToken(user.Id, TestData.HashOf("refresh-token-b")));

        // Act
        var act = () => context.SaveChangesAsync();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Ротация refresh-токена: после отзыва старого новый активный токен допустим
    /// </summary>
    [SkippableFact]
    public async Task RefreshTokenRotationShouldAllowNewActiveToken()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var user = TestData.CreateUser("TokenUser3", "token3@example.com");
        context.Add(user);
        await context.SaveChangesAsync();
        var oldToken = TestData.CreateRefreshToken(user.Id, TestData.HashOf("refresh-token-old"));
        context.Add(oldToken);
        await context.SaveChangesAsync();

        // Act
        oldToken.RevokedAt = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        await context.SaveChangesAsync();
        context.Add(TestData.CreateRefreshToken(user.Id, TestData.HashOf("refresh-token-new")));
        await context.SaveChangesAsync();

        // Assert
        var activeCount = await context.Set<RefreshToken>()
            .CountAsync(x => x.UserId == user.Id && x.RevokedAt == null);
        activeCount.Should().Be(1);
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Номер мягко удалённого созыва должен быть доступен для повторного использования
    /// </summary>
    [SkippableFact]
    public async Task SoftDeletedConvocationNumberShouldBeReusable()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var convocation = TestData.CreateConvocation(900, "TEST900");
        context.Add(convocation);
        await context.SaveChangesAsync();

        // Act
        convocation.DeletedAt = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        await context.SaveChangesAsync();
        context.Add(TestData.CreateConvocation(900, "TEST900A"));
        await context.SaveChangesAsync();

        // Assert
        var activeCount = await context.Set<Convocation>()
            .CountAsync(x => x.Number == 900 && x.DeletedAt == null);
        activeCount.Should().Be(1);
        await transaction.RollbackAsync();
    }
}