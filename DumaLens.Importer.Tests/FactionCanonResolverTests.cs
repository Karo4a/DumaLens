using DumaLens.Entities;
using DumaLens.Importer.Import;
using FluentAssertions;
using Xunit;

namespace DumaLens.Importer.Tests;

/// <summary>
/// Тесты канонизации названий фракций архива
/// </summary>
public sealed class FactionCanonResolverTests
{
    /// <summary>
    /// Исторические названия «ЕР», «Е-ЕР», «О-ЕР» относятся к канонической «Единой России»
    /// </summary>
    [Theory]
    [InlineData("ЕР")]
    [InlineData("Е-ЕР")]
    [InlineData("О-ЕР")]
    public void Resolve_UnitedRussiaAliases_ReturnCanonicalFaction(string title)
    {
        var resolver = CreateResolver();

        var faction = resolver.Resolve(title);

        faction.Should().NotBeNull();
        faction!.Code.Should().Be("ER");
    }

    /// <summary>
    /// «СР» и «СР-Р» относятся к канонической «Справедливой России»
    /// </summary>
    [Theory]
    [InlineData("СР")]
    [InlineData("СР-Р")]
    public void Resolve_JustRussiaAliases_ReturnCanonicalFaction(string title)
    {
        var resolver = CreateResolver();

        var faction = resolver.Resolve(title);

        faction.Should().NotBeNull();
        faction!.Code.Should().Be("SR");
    }

    /// <summary>
    /// Названия, совпадающие с справочником, находятся напрямую (включая «Вне фракций»)
    /// </summary>
    [Theory]
    [InlineData("КПРФ", "KPRF")]
    [InlineData("ЛДПР", "LDPR")]
    [InlineData("Вне фракций", "INDEPENDENT")]
    [InlineData("Новые люди", "NL")]
    public void Resolve_KnownTitle_ReturnsFactionByTitle(string title, string expectedCode)
    {
        var resolver = CreateResolver();

        var faction = resolver.Resolve(title);

        faction.Should().NotBeNull();
        faction!.Code.Should().Be(expectedCode);
    }

    /// <summary>
    /// Неизвестное название не сопоставляется ни с одной фракцией
    /// </summary>
    [Fact]
    public void Resolve_UnknownTitle_ReturnsNull()
    {
        var resolver = CreateResolver();

        resolver.Resolve("Неизвестная фракция").Should().BeNull();
    }

    private static FactionCanonResolver CreateResolver()
    {
        var factions = new[]
        {
            new Faction { Code = "ER", Title = "Единая Россия" },
            new Faction { Code = "KPRF", Title = "КПРФ" },
            new Faction { Code = "LDPR", Title = "ЛДПР" },
            new Faction { Code = "SR", Title = "Справедливая Россия" },
            new Faction { Code = "NL", Title = "Новые люди" },
            new Faction { Code = "INDEPENDENT", Title = "Вне фракций" },
        };

        return new FactionCanonResolver(
            factions.ToDictionary(x => x.Title, StringComparer.Ordinal),
            factions.ToDictionary(x => x.Code, StringComparer.Ordinal));
    }
}