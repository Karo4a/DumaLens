using DumaLens.Entities.Contracts.Enums;
using DumaLens.Parsing.Contracts.Enums;
using DumaLens.Parsing.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Parsing.Tests;

/// <summary>
/// Тесты парсера листинга сайта на реальных фикстурах (VIII, IX и III созывы)
/// </summary>
public class VoteListingParserTests
{
    private readonly VoteListingParser parser = new();

    /// <summary>
    /// Первая страница VIII созыва: 20 элементов, два формата счёта, пагинация до 835
    /// </summary>
    [Fact]
    public void Parse_ViiiFirstPage_ExtractsItemsAndPagination()
    {
        var listing = parser.Parse(FixtureFiles.Html("listing_viii_p1.html"));

        listing.Items.Should().HaveCount(20);
        listing.Issues.Should().BeEmpty();
        listing.PageNumbers.Should().BeEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 833, 834, 835]);
        listing.LastPageNumber.Should().Be(835);
        listing.TotalCount.Should().Be(16689);

        var first = listing.Items[0];
        first.SiteId.Should().Be(135715);
        first.Title.Should().Be("(за основу) О проекте порядка работы Государственной Думы на 27 июля 2026 года");
        first.VotedAt.Should().Be(new DateTimeOffset(2026, 7, 27, 10, 10, 17, TimeSpan.FromHours(3)));
        first.Outcome.Should().Be(VoteOutcome.Accepted);
        first.AcceptCount.Should().Be(421);
        first.AcceptPercent.Should().Be(94m);
        first.DeclineCount.Should().Be(0);
        first.AbstainCount.Should().Be(0);
    }

    /// <summary>
    /// Первая страница IX созыва: 20 элементов, пагинация до 22
    /// </summary>
    [Fact]
    public void Parse_IxFirstPage_ExtractsItems()
    {
        var listing = parser.Parse(FixtureFiles.Html("listing_ix_p1.html"));

        listing.Items.Should().HaveCount(20);
        listing.LastPageNumber.Should().Be(22);
        listing.TotalCount.Should().Be(437);

        var first = listing.Items[0];
        first.SiteId.Should().Be(136413);
        first.VotedAt.Should().Be(new DateTimeOffset(2026, 11, 12, 12, 12, 20, TimeSpan.FromHours(3)));
        first.Outcome.Should().Be(VoteOutcome.Accepted);
        first.AcceptCount.Should().Be(419);
        first.AcceptPercent.Should().Be(93m);
    }

    /// <summary>
    /// III созыв: отклонённое голосование 16384 без процента, счёт «За: 179, Против: 0, Воздержалось: 0»
    /// </summary>
    [Fact]
    public void Parse_IiiFirstPage_RejectedItemWithoutPercent()
    {
        var listing = parser.Parse(FixtureFiles.Html("listing_iii_p1.html"));

        listing.Items.Should().HaveCount(20);
        listing.LastPageNumber.Should().Be(760);
        listing.TotalCount.Should().Be(15198);

        var rejected = listing.Items.Single(i => i.SiteId == 16384);
        rejected.Title.Should().Be("(предл деп Зиятдиновой: включить ФЗ в повестку) О проекте порядка работы Государственной Думы на 28 ноября 2003 года");
        rejected.VotedAt.Should().Be(new DateTimeOffset(2003, 11, 28, 10, 18, 25, TimeSpan.FromHours(3)));
        rejected.Outcome.Should().Be(VoteOutcome.Rejected);
        rejected.AcceptCount.Should().Be(179);
        rejected.AcceptPercent.Should().BeNull();
        rejected.DeclineCount.Should().Be(0);
        rejected.AbstainCount.Should().Be(0);
    }

    /// <summary>
    /// Последняя страница VIII созыва: 9 элементов одной даты, номер последней страницы 835
    /// </summary>
    [Fact]
    public void Parse_ViiiLastPage_NineItemsOfOneDate()
    {
        var listing = parser.Parse(FixtureFiles.Html("listing_viii_last.html"));

        listing.Items.Should().HaveCount(9);
        listing.LastPageNumber.Should().Be(835);
        listing.TotalCount.Should().Be(16689);
        listing.Items.Select(i => i.SiteId).Should().Equal(Enumerable.Range(115589, 9).Select(n => (long)n));
        listing.Items.Should().OnlyContain(i => i.VotedAt.Date == new DateTime(2021, 10, 12));
    }

    /// <summary>
    /// Отфильтрованный листинг: 4 элемента в трёх дата-группах, пагинации нет
    /// </summary>
    [Fact]
    public void Parse_FilteredListing_GroupsByDate()
    {
        var listing = parser.Parse(FixtureFiles.Html("listing_viii_filtered.html"));

        listing.Items.Should().HaveCount(4);
        listing.Items.Select(i => i.SiteId).Should().Equal(126282L, 126255L, 126256L, 126091L);
        listing.Items[0].VotedAt.Date.Should().Be(new DateTime(2024, 3, 13));
        listing.Items[1].VotedAt.Date.Should().Be(new DateTime(2024, 3, 12));
        listing.Items[2].VotedAt.Date.Should().Be(new DateTime(2024, 3, 12));
        listing.Items[3].VotedAt.Date.Should().Be(new DateTime(2024, 2, 13));
        listing.PageNumbers.Should().BeEmpty();
        listing.LastPageNumber.Should().Be(1);
        listing.TotalCount.Should().Be(4);
        listing.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Пустая страница: нет элементов, номер последней страницы 1
    /// </summary>
    [Fact]
    public void Parse_EmptyHtml_NoItems()
    {
        var listing = parser.Parse("<html><body></body></html>");

        listing.Items.Should().BeEmpty();
        listing.LastPageNumber.Should().Be(1);
        listing.TotalCount.Should().BeNull();
        listing.Issues.Should().BeEmpty();
    }

    /// <summary>
    /// Нераспознанная дата группы: замечание InvalidDate, элементы группы пропускаются
    /// </summary>
    [Fact]
    public void Parse_InvalidGroupDate_AddsIssue()
    {
        const string html = "<html><body>"
            + "<div class=\"date-head\"><div>не дата</div></div>"
            + "<div class=\"item\"><div><div class=\"item-left\">"
            + "<div class=\"time\">10:00:00</div><a href=\"/vote/1\">Тест</a>"
            + "</div></div></div></body></html>";

        var listing = parser.Parse(html);

        listing.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidDate);
        listing.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Элемент без ссылки на голосование: замечание InvalidVoteId
    /// </summary>
    [Fact]
    public void Parse_ItemWithoutVoteLink_AddsIssue()
    {
        const string html = "<html><body>"
            + "<div class=\"date-head\"><div>13.03.2024</div></div>"
            + "<div class=\"item\"><div><div class=\"item-left\">"
            + "<div class=\"time\">10:00:00</div><a href=\"/other\">Тест</a>"
            + "</div></div></div></body></html>";

        var listing = parser.Parse(html);

        listing.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidVoteId);
        listing.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Идентификатор голосования вне диапазона long: элемент пропускается с замечанием InvalidVoteId, обход не падает
    /// </summary>
    [Fact]
    public void Parse_OverflowVoteId_SkipsItemWithIssue()
    {
        const string html = "<html><body>"
            + "<div class=\"date-head\"><div>13.03.2024</div></div>"
            + "<div class=\"item\"><div><div class=\"item-left\">"
            + "<div class=\"time\">10:00:00</div><a href=\"/vote/99999999999999999999999999\">Тест</a>"
            + "</div></div></div></body></html>";

        var listing = parser.Parse(html);

        listing.Issues.Should().ContainSingle(i => i.Code == ParseIssueCode.InvalidVoteId);
        listing.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Номер страницы пагинации вне диапазона int: ссылка игнорируется, остальные номера разбираются
    /// </summary>
    [Fact]
    public void Parse_OverflowPageNumber_IgnoresLink()
    {
        const string html = "<html><body>"
            + "<a href=\"/?convocation=AAAAAAA8&amp;page=99999999999999999999\">Дальше</a>"
            + "<a href=\"/?convocation=AAAAAAA8&amp;page=2\">2</a>"
            + "</body></html>";

        var listing = parser.Parse(html);

        listing.PageNumbers.Should().Equal(2);
        listing.LastPageNumber.Should().Be(2);
    }

    /// <summary>
    /// Счётчик «За» вне диапазона int: значение null, итог и остальные поля разбираются
    /// </summary>
    [Fact]
    public void Parse_OverflowAcceptCount_NullCount()
    {
        const string html = "<html><body>"
            + "<div class=\"date-head\"><div>13.03.2024</div></div>"
            + "<div class=\"item\"><div><div class=\"item-left\">"
            + "<div class=\"time\">10:00:00</div><a href=\"/vote/126282\">Тест</a></div>"
            + "<div class=\"item-right\"><div class=\"green\">принят</div>За: 99999999999999999999 чел</div>"
            + "</div></div></body></html>";

        var listing = parser.Parse(html);

        var item = listing.Items.Should().ContainSingle().Subject;
        item.AcceptCount.Should().BeNull();
        item.Outcome.Should().Be(VoteOutcome.Accepted);
    }
}