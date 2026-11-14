using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Parsing.Tests;

/// <summary>
/// Тесты парсера JSON архива vote-master.zip на реальных фикстурах
/// </summary>
public class VoteArchiveParserTests
{
    private readonly VoteArchiveParser parser = new();

    /// <summary>
    /// Файл 126282: 447 голосов, смещение +0300, ФИО и фракции из вложенного объекта deputy
    /// </summary>
    [Fact]
    public void Parse_RealFile126282_ExtractsAllFields()
    {
        var vote = parser.Parse(FixtureFiles.Json("archive_126282.json"));

        vote.SiteId.Should().Be(126282);
        vote.Source.Should().Be(VoteSource.Archive);
        vote.Title.Should().StartWith("(третье чтение) О проекте федерального закона № 500091-8 \"О внесении изменений в статью 29 Федерального закона \"О государственном");
        vote.VotedAt.Should().Be(new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(3)));
        vote.UtcOffsetMinutes.Should().Be(180);
        vote.ConvocationSiteCode.Should().BeNull();
        vote.HasResults.Should().BeTrue();
        vote.Issues.Should().BeEmpty();

        vote.DeputyVotes.Should().HaveCount(447);
        var first = vote.DeputyVotes[0];
        first.DeputySiteId.Should().Be(99113478);
        first.FullName.Should().Be("Абакаров Хизри Магомедович");
        first.FactionSiteId.Should().Be(72100024);
        first.FactionTitle.Should().Be("ЕР");
        first.Result.Should().Be(VoteResult.Accept);
        first.ResultRaw.Should().Be("accept");

        vote.DeputyVotes.Count(v => v.Result == VoteResult.Accept).Should().Be(396);
        vote.DeputyVotes.Count(v => v.Result == VoteResult.None).Should().Be(51);
        vote.DeputyVotes.Should().OnlyContain(v => v.Result != null);
    }

    /// <summary>
    /// Пустое голосование 17470: votes = [], HasResults = false
    /// </summary>
    [Fact]
    public void Parse_EmptyVote17470_HasNoResults()
    {
        var vote = parser.Parse(FixtureFiles.Json("archive_empty_17470.json"));

        vote.SiteId.Should().Be(17470);
        vote.HasResults.Should().BeFalse();
        vote.DeputyVotes.Should().BeEmpty();
        vote.Title.Should().Be("предл деп Билалова: перейти к п 31");
        vote.VotedAt.Should().Be(new DateTimeOffset(2002, 11, 15, 17, 1, 50, TimeSpan.FromHours(3)));
        vote.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Историческое смещение +0400 (2012 год) сохраняется, все 449 голосов — none
    /// </summary>
    [Fact]
    public void Parse_Plus0400_KeepsHistoricalOffset()
    {
        var vote = parser.Parse(FixtureFiles.Json("archive_plus0400.json"));

        vote.SiteId.Should().Be(87506);
        vote.Title.Should().Be("Проверка системы");
        vote.VotedAt.Should().Be(new DateTimeOffset(2012, 1, 13, 9, 17, 15, TimeSpan.FromHours(4)));
        vote.UtcOffsetMinutes.Should().Be(240);
        vote.DeputyVotes.Should().HaveCount(449);
        vote.DeputyVotes.Should().OnlyContain(v => v.Result == VoteResult.None);
    }

    /// <summary>
    /// Голосование I созыва 18642 (1994 год): все четыре кода результатов
    /// </summary>
    [Fact]
    public void Parse_OldVote18642_AllResultCodes()
    {
        var vote = parser.Parse(FixtureFiles.Json("archive_18642.json"));

        vote.SiteId.Should().Be(18642);
        vote.Title.Should().Be("за состав комиссии");
        vote.VotedAt.Should().Be(new DateTimeOffset(1994, 1, 11, 12, 40, 38, TimeSpan.FromHours(3)));
        vote.DeputyVotes.Should().HaveCount(444);
        vote.DeputyVotes.Count(v => v.Result == VoteResult.Accept).Should().Be(380);
        vote.DeputyVotes.Count(v => v.Result == VoteResult.Decline).Should().Be(12);
        vote.DeputyVotes.Count(v => v.Result == VoteResult.Abstain).Should().Be(10);
        vote.DeputyVotes.Count(v => v.Result == VoteResult.None).Should().Be(42);
        vote.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Архив и сайт дают одинаковые голоса по голосованию 126282
    /// </summary>
    [Fact]
    public void Parse_ConsistentWithSiteCard_SameDeputiesAndResults()
    {
        var site = new VotePageParser().Parse(FixtureFiles.Html("vote_126282.html"), 126282);
        var archive = parser.Parse(FixtureFiles.Json("archive_126282.json"));

        archive.Title.Should().Be(site.Title);
        archive.VotedAt.Should().Be(site.VotedAt);
        archive.DeputyVotes.Should().HaveCount(site.DeputyVotes.Count);

        var siteMap = site.DeputyVotes.ToDictionary(v => v.DeputySiteId);
        foreach (var archiveVote in archive.DeputyVotes)
        {
            var siteVote = siteMap[archiveVote.DeputySiteId];
            archiveVote.Result.Should().Be(siteVote.Result);
            archiveVote.FullName.Should().Be(siteVote.FullName);
        }
    }

    /// <summary>
    /// Некорректный JSON: замечание InvalidJson, пустой результат
    /// </summary>
    [Fact]
    public void Parse_InvalidJson_AddsIssue()
    {
        var vote = parser.Parse("{broken");

        vote.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidJson);
        vote.DeputyVotes.Should().BeEmpty();
    }

    /// <summary>
    /// JSON null: замечание InvalidJson
    /// </summary>
    [Fact]
    public void Parse_NullJson_AddsIssue()
    {
        var vote = parser.Parse("null");

        vote.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidJson);
    }

    /// <summary>
    /// Неизвестный код результата: Result = null, исходный код сохранён
    /// </summary>
    [Fact]
    public void Parse_UnknownResultCode_ResultIsNull()
    {
        const string json = "{\"id\":\"1\",\"title\":\"Тест\",\"datetime\":\"2024-01-01T00:00:00+0300\","
            + "\"votes\":[{\"deputy\":{\"id\":\"5\",\"name\":\"Иванов\",\"faction\":{\"id\":\"1\",\"title\":\"ЕР\"}},\"result\":\"weird\"}]}";

        var vote = parser.Parse(json);

        vote.DeputyVotes[0].Result.Should().BeNull();
        vote.DeputyVotes[0].ResultRaw.Should().Be("weird");
        vote.DeputyVotes[0].DeputySiteId.Should().Be(5);
    }

    /// <summary>
    /// Склеенное ФИО из архива разделяется на переходе строчная→заглавная
    /// </summary>
    [Fact]
    public void Parse_GluedFullName_SplitByCaseTransition()
    {
        const string json = "{\"id\":\"1\",\"title\":\"Тест\",\"datetime\":\"2024-01-01T00:00:00+0300\","
            + "\"votes\":[{\"deputy\":{\"id\":\"5\",\"name\":\"АбакаровХизри Магомедович\"},\"result\":\"accept\"}]}";

        var vote = parser.Parse(json);

        vote.DeputyVotes[0].FullName.Should().Be("Абакаров Хизри Магомедович");
    }

    /// <summary>
    /// Пустой объект: значения по умолчанию без замечаний (семантику проверяет валидатор)
    /// </summary>
    [Fact]
    public void Parse_EmptyObject_DefaultsWithoutIssues()
    {
        var vote = parser.Parse("{}");

        vote.SiteId.Should().Be(0);
        vote.Title.Should().BeEmpty();
        vote.VotedAt.Should().Be(default);
        vote.DeputyVotes.Should().BeEmpty();
        vote.Issues.Should().BeEmpty();
    }
}