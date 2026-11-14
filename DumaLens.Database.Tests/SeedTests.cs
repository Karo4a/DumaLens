using DumaLens.Database.Tests.Infrastructure;
using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Database.Tests;

/// <summary>
/// Тесты начального заполнения справочников (seed)
/// </summary>
[Collection("Database")]
public class SeedTests
{
    private readonly DatabaseFixture fixture;

    /// <summary>
    /// Конструктор
    /// </summary>
    public SeedTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Созывы должны быть заполнены: 9 созывов с кодами сайта и границами дат
    /// </summary>
    [SkippableFact]
    public async Task ConvocationsShouldBeSeeded()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();

        // Act
        var convocations = await context.Set<Convocation>().AsNoTracking()
            .OrderBy(x => x.Number)
            .ToListAsync();

        // Assert
        convocations.Should().HaveCount(9);
        convocations.Select(x => x.Number).Should().Equal(Enumerable.Range(1, 9));
        convocations.Select(x => x.SiteCode).Should().Equal(Enumerable.Range(1, 9).Select(n => $"AAAAAAA{n}"));
        convocations[0].DateFrom.Should().Be(new DateOnly(1994, 1, 11));
        convocations[8].DateFrom.Should().Be(new DateOnly(2026, 9, 30));
        convocations[8].DateTo.Should().BeNull();
        convocations[7].DateTo.Should().Be(convocations[8].DateFrom.AddDays(-1));
    }

    /// <summary>
    /// Канонические фракции должны быть заполнены: 25 фракций с фиксированными идентификаторами
    /// </summary>
    [SkippableFact]
    public async Task FactionsShouldBeSeeded()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();

        // Act
        var factions = await context.Set<Faction>().AsNoTracking().ToListAsync();

        // Assert
        factions.Should().HaveCount(25);
        factions.Select(x => x.Code).Should().Contain(new[] { "ER", "KPRF", "LDPR", "SR", "NL", "INDEPENDENT" });
        factions.Should().OnlyContain(x => x.Id == FactionsDefault.DefaultFactionToGuid(x.Code));
    }

    /// <summary>
    /// Фракционные периоды должны быть заполнены: 56 периодов, смена site-id и предшественники ЕР
    /// </summary>
    [SkippableFact]
    public async Task FactionPeriodsShouldBeSeeded()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();

        // Act
        var periods = await context.Set<FactionPeriod>().AsNoTracking().ToListAsync();

        // Assert
        periods.Should().HaveCount(56);

        var kprfId = FactionsDefault.DefaultFactionToGuid("KPRF");
        var kprfConvocations = periods.Where(x => x.FactionId == kprfId)
            .Select(x => x.ConvocationId)
            .ToHashSet();
        kprfConvocations.Should().HaveCount(8);
        foreach (var number in Enumerable.Range(1, 8))
        {
            kprfConvocations.Should().Contain(ConvocationsDefault.DefaultConvocationToGuid(number));
        }

        var srId = FactionsDefault.DefaultFactionToGuid("SR");
        periods.Where(x => x.FactionId == srId).Select(x => x.SiteId).Distinct()
            .Should().BeEquivalentTo(new long[] { 72100025, 72100027, 72100029 });

        var erId = FactionsDefault.DefaultFactionToGuid("ER");
        var thirdConvocationId = ConvocationsDefault.DefaultConvocationToGuid(3);
        periods.Where(x => x.FactionId == erId && x.ConvocationId == thirdConvocationId)
            .Select(x => x.Title)
            .Should().BeEquivalentTo(new[] { "Е-ЕР", "О-ЕР" });
    }

    /// <summary>
    /// Роли должны быть заполнены: 3 роли с фиксированными идентификаторами
    /// </summary>
    [SkippableFact]
    public async Task RolesShouldBeSeededWithFixedGuids()
    {
        // Arrange
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var context = fixture.CreateContext();

        // Act
        var roles = await context.Set<UserRole>().AsNoTracking().ToListAsync();

        // Assert
        roles.Should().HaveCount(3);
        foreach (var role in Enum.GetValues<Role>())
        {
            var entity = roles.Should().ContainSingle(x => x.Role == role).Subject;
            entity.Id.Should().Be(UserRolesDefault.DefaultRoleToGuid(role));
            entity.Description.Should().NotBeNullOrWhiteSpace();
        }
    }
}