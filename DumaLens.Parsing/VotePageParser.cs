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
/// Заголовок — h1 (HTML-сущности декодируются AngleSharp), дата — div.date-p span (московское время
/// с историческим смещением: +3/+4 до 27.03.2011, +4 в 2011–2014, +3 с 26.10.2014),
/// код созыва — из ссылок convocation=AAAAAAA8, голоса — JS-массив deputiesData
/// (regex + System.Text.Json; ФИО — sortName с приоритетом, id депутата — из url).
/// Пустые голосования имеют deputiesData = [] (HasResults = false).
/// </remarks>
public sealed partial class VotePageParser : IVotePageParser
{
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
            vote.VotedAt = new DateTimeOffset(votedAt, GetMoscowOffset(votedAt));
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
                    // Переполнение long (id вне диапазона) даёт 0 — карантин MissingDeputyId на валидации
                    DeputySiteId = deputyIdMatch.Success
                        && long.TryParse(deputyIdMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var deputyId)
                            ? deputyId
                            : 0,
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

    /// <summary>
    /// Историческое смещение Москвы для локального времени со страницы:
    /// до 27.03.2011 — +3 зимой/+4 летом (DST с 1981 года), 27.03.2011–26.10.2014 — +4 постоянно,
    /// с 26.10.2014 — +3 постоянно. Неоднозначный осенний час 02:00–03:00 трактуется как летний
    /// (первое вхождение); Windows-зона «Russian Standard Time» исторических правил не содержит
    /// </summary>
    private static TimeSpan GetMoscowOffset(DateTime local)
    {
        if (local >= new DateTime(2014, 10, 26, 2, 0, 0))
        {
            return TimeSpan.FromHours(3);
        }

        if (local >= new DateTime(2011, 3, 27, 2, 0, 0))
        {
            return TimeSpan.FromHours(4);
        }

        if (local.Year < 1981)
        {
            return TimeSpan.FromHours(3);
        }

        if (local.Year == 1981)
        {
            // Летнее время в СССР введено 01.04.1981, отменено 01.10.1981
            return local >= new DateTime(1981, 4, 1) && local < new DateTime(1981, 10, 1)
                ? TimeSpan.FromHours(4)
                : TimeSpan.FromHours(3);
        }

        var dstStart = LastSundayOfMonth(local.Year, 3).AddHours(2);
        var dstEnd = LastSundayOfMonth(local.Year, 10).AddHours(3);
        return local >= dstStart && local < dstEnd
            ? TimeSpan.FromHours(4)
            : TimeSpan.FromHours(3);
    }

    /// <summary>
    /// Последнее воскресенье месяца (границы летнего времени СССР/РФ до 2011 года)
    /// </summary>
    private static DateTime LastSundayOfMonth(int year, int month)
    {
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return lastDay.AddDays(-(int)lastDay.DayOfWeek);
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