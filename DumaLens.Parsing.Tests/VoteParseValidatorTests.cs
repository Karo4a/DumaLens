using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Contracts.Models;
using DumaLens.Parsing.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Parsing.Tests;

/// <summary>
/// Тесты валидатора результата разбора (причины карантина)
/// </summary>
public class VoteParseValidatorTests
{
    private readonly VoteParseValidator validator = new();

    /// <summary>
    /// Корректное голосование: замечаний нет
    /// </summary>
    [Fact]
    public void Validate_CorrectVote_NoIssues()
    {
        var issues = validator.Validate(MakeVote());

        issues.Should().BeEmpty();
    }

    /// <summary>
    /// Реальный файл архива 126282 проходит валидацию без замечаний
    /// </summary>
    [Fact]
    public void Validate_RealArchiveVote_NoIssues()
    {
        var vote = new VoteArchiveParser().Parse(FixtureFiles.Json("archive_126282.json"));

        var issues = validator.Validate(vote, new DateOnly(2021, 10, 12), new DateOnly(2026, 9, 29));

        issues.Should().BeEmpty();
    }

    /// <summary>
    /// Пустой заголовок: EmptyTitle
    /// </summary>
    [Fact]
    public void Validate_EmptyTitle_AddsIssue()
    {
        var vote = MakeVote();
        vote.Title = "   ";

        var issues = validator.Validate(vote);

        issues.Should().ContainSingle(i => i.Code == ParseIssueCode.EmptyTitle);
    }

    /// <summary>
    /// Некорректный идентификатор голосования: InvalidVoteId
    /// </summary>
    [Fact]
    public void Validate_ZeroSiteId_AddsIssue()
    {
        var vote = MakeVote();
        vote.SiteId = 0;

        var issues = validator.Validate(vote);

        issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidVoteId);
    }

    /// <summary>
    /// Отсутствующая дата: InvalidDate
    /// </summary>
    [Fact]
    public void Validate_DefaultDate_AddsIssue()
    {
        var vote = MakeVote();
        vote.VotedAt = default;

        var issues = validator.Validate(vote);

        issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidDate);
    }

    /// <summary>
    /// Число депутатов: 0 допустимо (пустое голосование), 430–460 допустимо, вне диапазона — карантин
    /// </summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(100, true)]
    [InlineData(429, true)]
    [InlineData(430, false)]
    [InlineData(447, false)]
    [InlineData(460, false)]
    [InlineData(461, true)]
    public void Validate_DeputyCount_ChecksRange(int count, bool expectIssue)
    {
        var issues = validator.Validate(MakeVote(count));

        issues.Where(i => i.Code == ParseIssueCode.DeputyCountOutOfRange)
            .Should().HaveCount(expectIssue ? 1 : 0);
    }

    /// <summary>
    /// Неизвестный код результата депутата: UnknownResultCode
    /// </summary>
    [Fact]
    public void Validate_UnknownResult_AddsIssue()
    {
        var vote = MakeVote();
        vote.DeputyVotes[0].Result = null;
        vote.DeputyVotes[0].ResultRaw = "weird";

        var issues = validator.Validate(vote);

        issues.Should().ContainSingle(i => i.Code == ParseIssueCode.UnknownResultCode);
    }

    /// <summary>
    /// Неизвлеченный идентификатор депутата: MissingDeputyId
    /// </summary>
    [Fact]
    public void Validate_MissingDeputyId_AddsIssue()
    {
        var vote = MakeVote();
        vote.DeputyVotes[0].DeputySiteId = 0;

        var issues = validator.Validate(vote);

        issues.Should().ContainSingle(i => i.Code == ParseIssueCode.MissingDeputyId);
    }

    /// <summary>
    /// Границы созыва: дата внутри диапазона и на границах допустима, вне — карантин
    /// </summary>
    [Theory]
    [InlineData(2021, 10, 12, 2026, 9, 29, false)]
    [InlineData(2024, 3, 13, 2024, 3, 13, false)]
    [InlineData(2016, 1, 1, 2016, 12, 31, true)]
    [InlineData(2024, 3, 14, 2026, 9, 29, true)]
    [InlineData(2021, 10, 12, 2024, 3, 12, true)]
    public void Validate_ConvocationRange_ChecksBoundaries(
        int fromYear, int fromMonth, int fromDay,
        int toYear, int toMonth, int toDay,
        bool expectIssue)
    {
        var issues = validator.Validate(
            MakeVote(),
            new DateOnly(fromYear, fromMonth, fromDay),
            new DateOnly(toYear, toMonth, toDay));

        issues.Where(i => i.Code == ParseIssueCode.DateOutOfConvocationRange)
            .Should().HaveCount(expectIssue ? 1 : 0);
    }

    /// <summary>
    /// Без границ созыва проверка диапазона не выполняется
    /// </summary>
    [Fact]
    public void Validate_NullRange_SkipsRangeCheck()
    {
        var issues = validator.Validate(MakeVote());

        issues.Should().NotContain(i => i.Code == ParseIssueCode.DateOutOfConvocationRange);
    }

    /// <summary>
    /// Структурные замечания парсера сохраняются в результате валидации
    /// </summary>
    [Fact]
    public void Validate_ParserIssues_Preserved()
    {
        var vote = MakeVote();
        vote.Issues = [new ParseIssue { Code = ParseIssueCode.MissingDeputiesData, Message = "нет массива" }];

        var issues = validator.Validate(vote);

        issues.Should().ContainSingle(i => i.Code == ParseIssueCode.MissingDeputiesData);
    }

    /// <summary>
    /// Собирает корректное голосование с заданным числом депутатов
    /// </summary>
    private static ParsedVote MakeVote(int deputyCount = 447)
    {
        var deputies = new List<ParsedDeputyVote>();
        for (var i = 1; i <= deputyCount; i++)
        {
            deputies.Add(new ParsedDeputyVote
            {
                DeputySiteId = i,
                FullName = $"Депутат {i}",
                Result = VoteResult.Accept,
                ResultRaw = "accept"
            });
        }

        return new ParsedVote
        {
            SiteId = 126282,
            Title = "(третье чтение) О проекте федерального закона № 500091-8",
            VotedAt = new DateTimeOffset(2024, 3, 13, 15, 1, 13, TimeSpan.FromHours(3)),
            Source = VoteSource.Archive,
            DeputyVotes = deputies
        };
    }
}