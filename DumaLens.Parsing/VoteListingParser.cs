using AngleSharp;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Contracts.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DumaLens.Parsing;

/// <summary>
/// Парсер листинга голосований сайта vote.duma.gov.ru
/// </summary>
/// <remarks>
/// Элементы сгруппированы по датам (div.date-head), время — div.time внутри элемента;
/// итог — класс блока результата (green — принят, red — отклонён); счёт «За» публикуется
/// в двух форматах: «За: 421 чел (94%)» и «За: 179, Против: 0, Воздержалось: 0».
/// Пагинация — ссылки с параметром page (20 элементов на страницу).
/// </remarks>
public sealed partial class VoteListingParser : IVoteListingParser
{
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    /// <inheritdoc/>
    public ParsedListing Parse(string html)
    {
        var listing = new ParsedListing();
        var items = new List<ParsedListingItem>();
        var issues = new List<ParseIssue>();

        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(request => request.Content(html)).GetAwaiter().GetResult();

        foreach (var head in document.QuerySelectorAll("div.date-head"))
        {
            var dateText = head.TextContent?.Trim();
            if (!DateOnly.TryParseExact(dateText, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                issues.Add(new ParseIssue
                {
                    Code = ParseIssueCode.InvalidDate,
                    Message = $"Не распознана дата группы листинга «{dateText}»"
                });
                continue;
            }

            var sibling = head.NextElementSibling;
            while (sibling is not null && !sibling.ClassList.Contains("date-head"))
            {
                if (sibling.ClassList.Contains("item"))
                {
                    var item = ParseItem(sibling, date, issues);
                    if (item is not null)
                    {
                        items.Add(item);
                    }
                }

                sibling = sibling.NextElementSibling;
            }
        }

        var pageNumbers = new SortedSet<int>();
        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href") ?? string.Empty;
            foreach (Match match in PageNumberRegex().Matches(href))
            {
                pageNumbers.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            }
        }

        listing.Items = items;
        listing.PageNumbers = pageNumbers.ToList();
        listing.Issues = issues;
        return listing;
    }

    /// <summary>
    /// Разбирает элемент листинга (div.item) в контексте даты группы
    /// </summary>
    private static ParsedListingItem? ParseItem(AngleSharp.Dom.IElement element, DateOnly date, List<ParseIssue> issues)
    {
        var anchor = element.QuerySelector("div.item-left a[href]");
        var href = anchor?.GetAttribute("href") ?? string.Empty;
        var idMatch = VoteIdRegex().Match(href);
        if (!idMatch.Success)
        {
            issues.Add(new ParseIssue
            {
                Code = ParseIssueCode.InvalidVoteId,
                Message = $"Не извлечён идентификатор голосования из ссылки «{href}»"
            });
            return null;
        }

        var timeText = element.QuerySelector("div.item-left div.time")?.TextContent?.Trim();
        var time = TimeOnly.MinValue;
        if (timeText is not null
            && TimeSpan.TryParseExact(timeText, ["hh\\:mm\\:ss", "hh\\:mm"], CultureInfo.InvariantCulture, out var timeSpan))
        {
            time = TimeOnly.FromTimeSpan(timeSpan);
        }

        var item = new ParsedListingItem
        {
            SiteId = long.Parse(idMatch.Groups[1].Value, CultureInfo.InvariantCulture),
            Title = HtmlText.Clean(anchor?.TextContent),
            VotedAt = new DateTimeOffset(date.ToDateTime(time), MoscowOffset)
        };

        var right = element.QuerySelector("div.item-right");
        if (right is null)
        {
            return item;
        }

        if (right.QuerySelector("div.green") is not null)
        {
            item.Outcome = VoteOutcome.Accepted;
        }
        else if (right.QuerySelector("div.red") is not null)
        {
            item.Outcome = VoteOutcome.Rejected;
        }

        var text = right.TextContent ?? string.Empty;
        item.AcceptCount = ParseInt(AcceptCountRegex(), text);
        item.DeclineCount = ParseInt(DeclineCountRegex(), text);
        item.AbstainCount = ParseInt(AbstainCountRegex(), text);

        var percentMatch = AcceptPercentRegex().Match(text);
        if (percentMatch.Success
            && decimal.TryParse(percentMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var percent))
        {
            item.AcceptPercent = percent;
        }

        return item;
    }

    private static int? ParseInt(Regex regex, string text)
    {
        var match = regex.Match(text);
        return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }

    [GeneratedRegex(@"/vote/(\d+)")]
    private static partial Regex VoteIdRegex();

    [GeneratedRegex(@"[?&]page=(\d+)")]
    private static partial Regex PageNumberRegex();

    [GeneratedRegex(@"За:\s*(\d+)")]
    private static partial Regex AcceptCountRegex();

    [GeneratedRegex(@"Против:\s*(\d+)")]
    private static partial Regex DeclineCountRegex();

    [GeneratedRegex(@"Воздержалось:\s*(\d+)")]
    private static partial Regex AbstainCountRegex();

    [GeneratedRegex(@"\((\d+(?:[.,]\d+)?)%\)")]
    private static partial Regex AcceptPercentRegex();
}