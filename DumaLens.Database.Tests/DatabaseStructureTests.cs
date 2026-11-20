using DumaLens.Database.Tests.Infrastructure;
using DumaLens.Entities;
using EfSchemaCompare;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace DumaLens.Database.Tests;

/// <summary>
/// Тесты соответствия структуры базы данных EF-модели
/// </summary>
[Collection("Database")]
public class DatabaseStructureTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DatabaseStructureTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Имя одноразовой базы должно получаться из шаблона один раз (без двойного префикса dumalens_)
    /// </summary>
    [SkippableFact]
    public void FixtureDatabaseNameShouldMatchTemplate()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);

        // Act
        var database = new NpgsqlConnectionStringBuilder(fixture.ConnectionString).Database;

        // Assert
        database.Should().MatchRegex("^dumalens_[0-9a-f]{32}$");
    }

    /// <summary>
    /// База данных должна соответствовать EF-модели (таблицы, колонки, индексы, ключи)
    /// </summary>
    [SkippableFact]
    public void DatabaseShouldMatchEfModel()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        using var context = fixture.CreateContext();
        var comparer = new CompareEfSql();

        // Act
        comparer.CompareEfWithDb(context);

        // Assert
        comparer.GetAllErrors.Should().BeEmpty();
    }

    /// <summary>
    /// Таблица должна существовать в базе данных
    /// </summary>
    [SkippableTheory]
    [InlineData("Bills")]
    [InlineData("Convocations")]
    [InlineData("Deputies")]
    [InlineData("DeputyFactionMemberships")]
    [InlineData("DeputyVotes")]
    [InlineData("Factions")]
    [InlineData("FactionPeriods")]
    [InlineData("ImportQuarantine")]
    [InlineData("RefreshTokens")]
    [InlineData("Roles")]
    [InlineData("Users")]
    [InlineData("Votes")]
    [InlineData("VoteFactionStats")]
    public async Task TableShouldExist(string tableName)
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();

        // Act
        var count = await context.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public' AND table_name = {tableName}")
            .SingleAsync();

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// RefreshTokens.UserId ссылается на Users.Id (внешний ключ FK_RefreshTokens_Users_UserId)
    /// </summary>
    [SkippableFact]
    public async Task RefreshTokensShouldHaveForeignKeyToUsers()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();

        // Act
        var count = await context.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM information_schema.table_constraints WHERE table_schema = 'public' AND table_name = 'RefreshTokens' AND constraint_type = 'FOREIGN KEY' AND constraint_name = 'FK_RefreshTokens_Users_UserId'")
            .SingleAsync();

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// Физическое удаление пользователя каскадно удаляет его refresh-токены
    /// </summary>
    [SkippableFact]
    public async Task DeletingUserShouldCascadeRefreshTokens()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();
        var roleId = await context.Set<UserRole>().Select(x => x.Id).FirstAsync();
        var user = new User
        {
            Login = "fk_cascade",
            LoginLowerCase = "fk_cascade",
            Email = string.Empty,
            EmailLowerCase = string.Empty,
            Name = "Каскад",
            PasswordSalt = "salt",
            PasswordHash = "hash",
            SecurityStamp = Guid.NewGuid(),
            RoleId = roleId,
        };
        context.Set<User>().Add(user);
        await context.SaveChangesAsync();
        var token = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "fk_hash_" + Guid.NewGuid().ToString("N"),
            SecurityStamp = user.SecurityStamp,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        };
        context.Set<RefreshToken>().Add(token);
        await context.SaveChangesAsync();

        // Act
        context.Set<User>().Remove(user);
        await context.SaveChangesAsync();

        // Assert
        var remaining = await context.Set<RefreshToken>().CountAsync(x => x.UserId == user.Id);
        remaining.Should().Be(0);
    }
}