using DumaLens.Web.Components.Shared;
using FluentAssertions;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Тесты форматирования UI: канонический порядок фракций и русская плюрализация
/// </summary>
public class UiFormatTests
{
    [Theory]
    [InlineData("ER", 0)]
    [InlineData("KPRF", 1)]
    [InlineData("NL", 4)]
    [InlineData("INDEPENDENT", 5)]
    [InlineData("APR", 50)]
    [InlineData("", 100)]
    [InlineData(null, 100)]
    public void FactionSortKey_CanonicalOrder(string? code, int expected)
    {
        UiFormat.FactionSortKey(code).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "голосований")]
    [InlineData(1, "голосование")]
    [InlineData(2, "голосования")]
    [InlineData(5, "голосований")]
    [InlineData(11, "голосований")]
    [InlineData(14, "голосований")]
    [InlineData(21, "голосование")]
    [InlineData(111, "голосований")]
    [InlineData(110714, "голосований")]
    [InlineData(110704, "голосования")]
    public void Plural_RussianRules(int count, string expected)
    {
        UiFormat.Plural(count, "голосование", "голосования", "голосований").Should().Be(expected);
    }
}