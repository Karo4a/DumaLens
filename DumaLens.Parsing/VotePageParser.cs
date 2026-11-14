using AngleSharp;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Contracts.Models;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DumaLens.Parsing;

/// <summary>
/// Парсер карточки голосования сайта vote.duma.gov.ru (/vote/{id})
/// </summary>
/// <remarks>
/// Заголовок — h1 (HTML-сущности декодируются AngleSharp), дата — div.date-p span (московское время),
/// код созыва — из ссылок convocation=AAAAAAA8, голоса — JS-массив deputiesData
/// (regex + System.Text.Json; ФИО — sortName с приоритетом, id депутата — из url).
/// Пустые голосования имеют deputiesData = [] (HasResults = false).
/// </remarks>
public sealed partial class VotePageParser : IVotePageParser
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    /// <inheritdoc/>
    public ParsedVote Parse(string html, long siteId)
    {
        var vote = new ParsedVote { SiteId = siteId, Source = VoteSource.Site };
        var issues = new List<ParseIssue>();

        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(request => request.Content(html)).GetAwaiter().GetResult();

        var heading = document.QuerySelector("h1");
        vote.TitleRaw = heading?.TextContent ?? string.Empty;
        vote.Title = HtmlText.Clean(vote.TitleRaw);

        var dateText = document.QuerySelector("div.date-p span")?.TextContent;
        if (DateTime.TryParseExact(
                dateText?.Trim(),
                "dd.MM.yyyy HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var votedAt))
        {
            vote.VotedAt = new DateTimeOffset(votedAt, MoscowOffset);
        }

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            var convocationMatch = ConvocationRegex().Match(href ?? string.Empty);
            if (convocationMatch.Success)
            {
                vote.ConvocationSiteCode = convocationMatch.Groups[1].Value;
                break;
            }
        }

        var deputiesMatch = DeputiesDataRegex().Match(html);
        if (!deputiesMatch.Success)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.MissingDeputiesData,
                Message = $"На странице голосования {siteId} не найден массив deputiesData"
            });
            vote.Issues = issues;
            return vote;
        }

        List<SiteDeputy>? deputies;
        try
        {
            deputies = JsonSerializer.Deserialize<List<SiteDeputy>>(deputiesMatch.Groups[1].Value);
        }
        catch (JsonException ex)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.InvalidJson,
                Message = $"Некорректный JSON deputiesData голосования {siteId}: {ex.Message}"
            });
            vote.Issues = issues;
            return vote;
        }

        var deputyVotes = new List<ParsedDeputyVote>();
        if (deputies is not null)
        {
            foreach (var deputy in deputies)
            {
                var deputyIdMatch = DeputyIdRegex().Match(deputy.Url ?? string.Empty);
                var fullName = !string.IsNullOrWhiteSpace(deputy.SortName)
                    ? deputy.SortName
                    : HtmlTagsRegex().Replace(deputy.Name ?? string.Empty, string.Empty);

                deputyVotes.Add(new ParsedDeputyVote
                {
                    DeputySiteId = deputyIdMatch.Success ? long.Parse(deputyIdMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0,
                    FullName = HtmlText.CleanFullName(fullName),
                    FactionSiteId = long.TryParse(deputy.FactionCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var factionId)
                        ? factionId
                        : null,
                    FactionTitle = string.IsNullOrWhiteSpace(deputy.Faction) ? null : deputy.Faction.Trim(),
                    ResultRaw = deputy.Result ?? string.Empty,
                    Result = MapResult(deputy.Result)
                });
            }
        }

        vote.DeputyVotes = deputyVotes;
        vote.Issues = issues;
        return vote;
    }

    /// <summary>
    /// Маппинг кодов сайта: for/against/abstain/absent → канонический результат
    /// </summary>
    private static VoteResult? MapResult(string? code)
    {
        return code?.ToLowerInvariant() switch
        {
            "for" => VoteResult.Accept,
            "against" => VoteResult.Decline,
            "abstain" => VoteResult.Abstain,
            "absent" => VoteResult.None,
            _ => null
        };
    }

    [GeneratedRegex(@"deputiesData\s*=\s*(\[.*?\]);", RegexOptions.Singleline)]
    private static partial Regex DeputiesDataRegex();

    [GeneratedRegex(@"convocation=(AAAAAAA\d)")]
    private static partial Regex ConvocationRegex();

    [GeneratedRegex(@"deputy=(\d+)")]
    private static partial Regex DeputyIdRegex();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex HtmlTagsRegex();

    /// <summary>
    /// DTO элемента массива deputiesData (ключ letter дублируется в JSON — не используется)
    /// </summary>
    private sealed class SiteDeputy
    {
        /// <summary>
        /// ФИО для сортировки (приоритетный источник ФИО)
        /// </summary>
        [JsonPropertyName("sortName")]
        public string? SortName { get; set; }

        /// <summary>
        /// ФИО с HTML-разметкой (резервный источник)
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Ссылка с идентификатором депутата (deputy=…)
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Короткое название фракции
        /// </summary>
        [JsonPropertyName("faction")]
        public string? Faction { get; set; }

        /// <summary>
        /// Идентификатор фракции на сайте (строкой)
        /// </summary>
        [JsonPropertyName("factionCode")]
        public string? FactionCode { get; set; }

        /// <summary>
        /// Код результата (for/against/abstain/absent)
        /// </summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }
}