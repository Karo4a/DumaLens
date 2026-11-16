using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Parsing.Tests;

/// <summary>
/// Тесты парсера карточки голосования сайта на реальных фикстурах
/// </summary>
public class VotePageParserTests
{
    private readonly VotePageParser parser = new();

    /// <summary>
    /// Карточка 126282: заголовок с декодированными сущностями, дата, созыв, 447 голосов
    /// </summary>
    [Fact]
    public void Parse_RealCard126282_ExtractsAllFields()
    {
        var vote = parser.Parse(FixtureFiles.Html("vote_126282.html"), 126282);

        vote.SiteId.Should().Be(126282);
        vote.Source.Should().Be(VoteSource.Site);
        vote.Title.Should().StartWith("(третье чтение) О проекте федерального закона № 500091-8 \"О внесении изменений в статью 29 Федерального закона \"О государственном");
        vote.Title.Should().NotContain("&quot;");
        vote.TitleRaw.Should().StartWith("(третье чтение) О проекте федерального закона № 500091-8");
        vote.VotedAt.Should().Be(new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(3)));
        vote.UtcOffsetMinutes.Should().Be(180);
        vote.ConvocationSiteCode.Should().Be("AAAAAAA8");
        vote.HasResults.Should().BeTrue();
        vote.Issues.Should().BeEmpty();

        vote.DeputyVotes.Should().HaveCount(447);
        var first = vote.DeputyVotes[0];
        first.DeputySiteId.Should().Be(99113478);
        first.FullName.Should().Be("Абакаров Хизри Магомедович");
        first.FactionSiteId.Should().Be(72100024);
        first.FactionTitle.Should().Be("ЕР");
        first.Result.Should().Be(VoteResult.Accept);
        first.ResultRaw.Should().Be("for");

        vote.DeputyVotes.Count(v => v.Result == VoteResult.Accept).Should().Be(396);
        vote.DeputyVotes.Count(v => v.Result == VoteResult.None).Should().Be(51);
        vote.DeputyVotes.Should().OnlyContain(v => v.Result != null);
        vote.DeputyVotes.Should().OnlyContain(v => v.DeputySiteId > 0);
    }

    /// <summary>
    /// Живая карточка 135715 (VIII созыв, «за основу», 433 депутата): присваивание
    /// deputiesData после var-списка, JSON с полями letter/name — разметка стабильна с Э2
    /// </summary>
    [Fact]
    public void Parse_LiveCard135715_ExtractsAllFields()
    {
        var vote = parser.Parse(FixtureFiles.Html("vote_135715.html"), 135715);

        vote.SiteId.Should().Be(135715);
        vote.Title.Should().Be("(за основу) О проекте порядка работы Государственной Думы на 27 июля 2026 года");
        vote.VotedAt.Should().Be(new DateTimeOffset(2026, 7, 27, 10, 10, 17, TimeSpan.FromHours(3)));
        vote.ConvocationSiteCode.Should().Be("AAAAAAA8");
        vote.HasResults.Should().BeTrue();
        vote.Issues.Should().BeEmpty();

        vote.DeputyVotes.Should().HaveCount(433);
        var first = vote.DeputyVotes[0];
        first.DeputySiteId.Should().Be(99113478);
        first.FullName.Should().Be("Абакаров Хизри Магомедович");
        first.FactionSiteId.Should().Be(72100024);
        first.FactionTitle.Should().Be("ЕР");
        first.Result.Should().Be(VoteResult.None);
        first.ResultRaw.Should().Be("absent");
        vote.DeputyVotes.Should().OnlyContain(v => v.Result != null);
        vote.DeputyVotes.Should().OnlyContain(v => v.DeputySiteId > 0);
    }

    /// <summary>
    /// Пустое голосование 17470: deputiesData = [], HasResults = false, без замечаний
    /// </summary>
    [Fact]
    public void Parse_EmptyVote17470_HasNoResults()
    {
        var vote = parser.Parse(FixtureFiles.Html("vote_17470.html"), 17470);

        vote.HasResults.Should().BeFalse();
        vote.DeputyVotes.Should().BeEmpty();
        vote.Issues.Should().BeEmpty();
        vote.Title.Should().Be("предл деп Билалова: перейти к п 31");
        vote.VotedAt.Should().Be(new DateTimeOffset(2002, 11, 15, 17, 1, 50, TimeSpan.FromHours(3)));
        vote.ConvocationSiteCode.Should().BeNull();
    }

    /// <summary>
    /// Процедурное голосование 16384 (III созыв): код созыва AAAAAAA3, все коды результатов известны
    /// </summary>
    [Fact]
    public void Parse_ProceduralVote16384_ExtractsConvocationAndResults()
    {
        var vote = parser.Parse(FixtureFiles.Html("vote_procedural.html"), 16384);

        vote.Title.Should().Be("(предл деп Зиятдиновой: включить ФЗ в повестку) О проекте порядка работы Государственной Думы на 28 ноября 2003 года");
        vote.ConvocationSiteCode.Should().Be("AAAAAAA3");
        vote.VotedAt.Should().Be(new DateTimeOffset(2003, 11, 28, 10, 18, 25, TimeSpan.FromHours(3)));
        vote.HasResults.Should().BeTrue();
        vote.DeputyVotes.Should().OnlyContain(v => v.Result != null);
        vote.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Все четыре кода сайта маппятся в канонические результаты
    /// </summary>
    [Fact]
    public void Parse_AllResultCodes_MappedToCanonical()
    {
        const string deputies = "["
            + "{\"sortName\":\"А\",\"url\":\"/?deputy=1\",\"result\":\"for\"},"
            + "{\"sortName\":\"Б\",\"url\":\"/?deputy=2\",\"result\":\"against\"},"
            + "{\"sortName\":\"В\",\"url\":\"/?deputy=3\",\"result\":\"abstain\"},"
            + "{\"sortName\":\"Г\",\"url\":\"/?deputy=4\",\"result\":\"absent\"}]";

        var vote = parser.Parse(Card(deputies), 1);

        vote.DeputyVotes.Select(v => v.Result).Should().Equal(
            VoteResult.Accept, VoteResult.Decline, VoteResult.Abstain, VoteResult.None);
        vote.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Неизвестный код результата сохраняется в ResultRaw, Result = null
    /// </summary>
    [Fact]
    public void Parse_UnknownResultCode_ResultIsNull()
    {
        const string deputies = "[{\"sortName\":\"Иванов Иван Иванович\",\"url\":\"/?deputy=1\",\"result\":\"weird\"}]";

        var vote = parser.Parse(Card(deputies), 1);

        vote.DeputyVotes[0].Result.Should().BeNull();
        vote.DeputyVotes[0].ResultRaw.Should().Be("weird");
    }

    /// <summary>
    /// Отсутствие url депутата: DeputySiteId = 0 (причина карантина на этапе валидации)
    /// </summary>
    [Fact]
    public void Parse_MissingDeputyUrl_DeputySiteIdIsZero()
    {
        const string deputies = "[{\"sortName\":\"Иванов\",\"result\":\"for\"}]";

        var vote = parser.Parse(Card(deputies), 1);

        vote.DeputyVotes[0].DeputySiteId.Should().Be(0);
    }

    /// <summary>
    /// Идентификатор депутата вне диапазона long: DeputySiteId = 0 (карантин на валидации), без исключения
    /// </summary>
    [Fact]
    public void Parse_DeputyIdOverflow_DeputySiteIdIsZero()
    {
        const string deputies = "[{\"sortName\":\"Иванов Иван Иванович\",\"url\":\"/?deputy=99999999999999999999\",\"result\":\"for\"}]";

        var vote = parser.Parse(Card(deputies), 1);

        vote.DeputyVotes[0].DeputySiteId.Should().Be(0);
        vote.DeputyVotes[0].Result.Should().Be(VoteResult.Accept);
    }

    /// <summary>
    /// Без sortName ФИО берётся из name с удалением HTML-разметки
    /// </summary>
    [Fact]
    public void Parse_MissingSortName_FallsBackToNameWithoutHtml()
    {
        const string deputies = "[{\"name\":\"<b>Петров</b>  Пётр\",\"url\":\"/?deputy=2\",\"result\":\"against\"}]";

        var vote = parser.Parse(Card(deputies), 1);

        vote.DeputyVotes[0].FullName.Should().Be("Петров Пётр");
        vote.DeputyVotes[0].Result.Should().Be(VoteResult.Decline);
    }

    /// <summary>
    /// Страница без deputiesData: замечание MissingDeputiesData
    /// </summary>
    [Fact]
    public void Parse_MissingDeputiesData_AddsIssue()
    {
        var vote = parser.Parse("<html><body><h1>Тест</h1></body></html>", 1);

        vote.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.MissingDeputiesData);
        vote.DeputyVotes.Should().BeEmpty();
    }

    /// <summary>
    /// Некорректный JSON deputiesData: замечание InvalidJson
    /// </summary>
    [Fact]
    public void Parse_InvalidDeputiesJson_AddsIssue()
    {
        var vote = parser.Parse(Card("[{\"sortName\": }];"), 1);

        vote.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidJson);
    }

    /// <summary>
    /// Страница без блока даты: VotedAt остаётся значением по умолчанию
    /// </summary>
    [Fact]
    public void Parse_MissingDate_VotedAtIsDefault()
    {
        var html = "<html><body><h1>Тест</h1><script>var deputiesData = [];</script></body></html>";

        var vote = parser.Parse(html, 1);

        vote.VotedAt.Should().Be(default);
    }

    /// <summary>
    /// HTML-сущности заголовка декодируются AngleSharp
    /// </summary>
    [Fact]
    public void Parse_TitleEntities_Decoded()
    {
        var html = "<html><body><h1>О проекте &quot;Закона&quot; &amp; поправок</h1>"
            + "<script>var deputiesData = [];</script></body></html>";

        var vote = parser.Parse(html, 1);

        vote.Title.Should().Be("О проекте \"Закона\" & поправок");
    }

    /// <summary>
    /// Склеенное ФИО разделяется на переходе строчная→заглавная («АбакаровХизри» → «Абакаров Хизри»)
    /// </summary>
    [Fact]
    public void Parse_GluedFullName_SplitByCaseTransition()
    {
        const string deputies = "[{\"sortName\":\"АбакаровХизри Магомедович\",\"url\":\"/?deputy=1\",\"result\":\"for\"}]";

        var vote = parser.Parse(Card(deputies), 1);

        vote.DeputyVotes[0].FullName.Should().Be("Абакаров Хизри Магомедович");
    }

    /// <summary>
    /// Смещение Москвы историческое: +3/+4 (DST) до 27.03.2011, +4 постоянно в 2011–2014,
    /// +3 с 26.10.2014; VotedAt и UtcOffsetMinutes согласованы
    /// </summary>
    [Theory]
    [InlineData("15.01.2010 12:00:00", 3)]
    [InlineData("15.07.2010 12:00:00", 4)]
    [InlineData("28.03.2010 01:59:00", 3)]
    [InlineData("28.03.2010 03:00:00", 4)]
    [InlineData("31.10.2010 03:30:00", 3)]
    [InlineData("15.01.2012 12:00:00", 4)]
    [InlineData("15.06.2012 12:00:00", 4)]
    [InlineData("27.03.2011 01:59:00", 3)]
    [InlineData("27.03.2011 03:00:00", 4)]
    [InlineData("26.10.2014 00:30:00", 4)]
    [InlineData("26.10.2014 02:30:00", 3)]
    [InlineData("13.03.2024 15:01:13", 3)]
    public void Parse_HistoricalMoscowOffset(string dateText, int expectedOffsetHours)
    {
        var vote = parser.Parse(CardWithDate(dateText, "[]"), 1);

        vote.VotedAt.Should().NotBe(default);
        vote.UtcOffsetMinutes.Should().Be(expectedOffsetHours * 60);
        vote.VotedAt.Offset.Should().Be(TimeSpan.FromHours(expectedOffsetHours));
    }

    /// <summary>
    /// Собирает минимальную карточку с заданным массивом deputiesData
    /// </summary>
    private static string Card(string deputiesJs)
    {
        return CardWithDate("13.03.2024 15:01:13", deputiesJs);
    }

    /// <summary>
    /// Собирает минимальную карточку с заданными датой и массивом deputiesData
    /// </summary>
    private static string CardWithDate(string dateText, string deputiesJs)
    {
        return "<html><body><h1>Тестовое голосование</h1>"
            + $"<div class=\"date-p\"><span>{dateText}</span></div>"
            + $"<script>var deputiesData = {deputiesJs};</script></body></html>";
    }
}