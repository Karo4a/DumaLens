using DumaLens.Repositories.Search;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты построителя tsquery (Э5): синтаксис веб-поиска, префиксные лексемы, очистка ввода
/// </summary>
public sealed class RussianTsQueryBuilderTests
{
    /// <summary>
    /// Пустой ввод не даёт FTS-фильтра
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildEmptyInputShouldReturnNull(string? query)
    {
        RussianTsQueryBuilder.Build(query).Should().BeNull();
    }

    /// <summary>
    /// Одно слово — префиксная лексема
    /// </summary>
    [Fact]
    public void BuildSingleWordShouldAddPrefix()
    {
        RussianTsQueryBuilder.Build("алкоголь").Should().Be("алкоголь:*");
    }

    /// <summary>
    /// Несколько слов — И
    /// </summary>
    [Fact]
    public void BuildWordsShouldJoinWithAnd()
    {
        RussianTsQueryBuilder.Build("алкоголь налог").Should().Be("алкоголь:* & налог:*");
    }

    /// <summary>
    /// Фраза в кавычках — соседство лексем
    /// </summary>
    [Fact]
    public void BuildPhraseShouldJoinWithFollows()
    {
        RussianTsQueryBuilder.Build("\"розничной продажи\"").Should().Be("(розничной:* <-> продажи:*)");
    }

    /// <summary>
    /// Минус-слово — отрицание
    /// </summary>
    [Fact]
    public void BuildMinusWordShouldNegate()
    {
        RussianTsQueryBuilder.Build("алкоголь -несовершеннолетним").Should().Be("алкоголь:* & !несовершеннолетним:*");
    }

    /// <summary>
    /// Минус-фраза — отрицание соседства в скобках
    /// </summary>
    [Fact]
    public void BuildMinusPhraseShouldNegateGroup()
    {
        RussianTsQueryBuilder.Build("алкоголь -\"розничной продажи\"").Should().Be("алкоголь:* & !(розничной:* <-> продажи:*)");
    }

    /// <summary>
    /// Минус с пробелом перед словом/фразой — отрицание сохраняется (привычный набор «- слово»),
    /// а не инвертируется в положительный терм
    /// </summary>
    [Theory]
    [InlineData("алкоголь - пиво", "алкоголь:* & !пиво:*")]
    [InlineData("алкоголь -  \"розничной продажи\"", "алкоголь:* & !(розничной:* <-> продажи:*)")]
    [InlineData("- пиво", "!пиво:*")]
    [InlineData("алкоголь -", "алкоголь:*")]
    public void BuildMinusWithSpaceShouldNegate(string query, string expected)
    {
        RussianTsQueryBuilder.Build(query).Should().Be(expected);
    }

    /// <summary>
    /// OR и | — ИЛИ; И связывает сильнее ИЛИ
    /// </summary>
    [Theory]
    [InlineData("алкоголь OR налог", "алкоголь:* | налог:*")]
    [InlineData("алкоголь | налог", "алкоголь:* | налог:*")]
    [InlineData("алкоголь or налог", "алкоголь:* | налог:*")]
    [InlineData("а б OR в", "а:* & б:* | в:*")]
    [InlineData("а OR б в", "а:* | б:* & в:*")]
    public void BuildOrShouldJoinWithOr(string query, string expected)
    {
        RussianTsQueryBuilder.Build(query).Should().Be(expected);
    }

    /// <summary>
    /// Служебные символы tsquery во вводе не ломают синтаксис
    /// </summary>
    [Theory]
    [InlineData("алкоголь!!!", "алкоголь:*")]
    [InlineData("abc!!!def", "(abc:* & def:*)")]
    [InlineData("алкоголь & | ! ( )", "алкоголь:*")]
    [InlineData("спиртосодержащей", "спиртосодержащей:*")]
    [InlineData("500091-8", "500091-8:*")]
    public void BuildShouldSanitizeSpecialCharacters(string query, string expected)
    {
        RussianTsQueryBuilder.Build(query).Should().Be(expected);
    }

    /// <summary>
    /// Ввод без лексем даёт пустой tsquery (совпадений не будет), а не null
    /// </summary>
    [Theory]
    [InlineData("!!! ???")]
    [InlineData("- --")]
    [InlineData("OR")]
    public void BuildWithoutLexemesShouldReturnEmpty(string query)
    {
        RussianTsQueryBuilder.Build(query).Should().BeEmpty();
    }

    /// <summary>
    /// Стоп-слова сохраняются в тексте — их отбрасывает PostgreSQL при to_tsquery
    /// </summary>
    [Fact]
    public void BuildStopWordsShouldKeepThemForPostgres()
    {
        RussianTsQueryBuilder.Build("и в на").Should().Be("и:* & в:* & на:*");
    }

    /// <summary>
    /// Незакрытая кавычка — фраза до конца строки
    /// </summary>
    [Fact]
    public void BuildUnclosedQuoteShouldReadPhraseToEnd()
    {
        RussianTsQueryBuilder.Build("\"розничной продажи").Should().Be("(розничной:* <-> продажи:*)");
    }

    /// <summary>
    /// Только минус-слово — отрицание без положительной части (как в веб-поиске)
    /// </summary>
    [Fact]
    public void BuildOnlyMinusShouldNegate()
    {
        RussianTsQueryBuilder.Build("-алкоголь").Should().Be("!алкоголь:*");
    }
}