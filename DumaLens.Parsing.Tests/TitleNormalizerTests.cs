using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Parsing.Tests.Infrastructure;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace DumaLens.Parsing.Tests;

/// <summary>
/// Тесты нормализатора заголовков: все варианты стадий словаря §2.2 и фактические заголовки архива
/// </summary>
public class TitleNormalizerTests
{
    private readonly TitleNormalizer normalizer = new();

    /// <summary>
    /// Словарь стадий и реальные заголовки архива: стадия и номер законопроекта
    /// </summary>
    [Theory]
    [InlineData("(первое чтение) О проекте федерального закона № 500091-8", VoteStage.FirstReading, "500091-8")]
    [InlineData("(1 чтение) О проекте федерального закона № 10962-5", VoteStage.FirstReading, "10962-5")]
    [InlineData("(1 чт) О проекте федерального закона № 10962-5", VoteStage.FirstReading, "10962-5")]
    [InlineData("законопроект в 1 чтении", VoteStage.FirstReading, null)]
    [InlineData("ФЗ о внесении изменений 1-чт", VoteStage.FirstReading, null)]
    [InlineData("(второе чтение) О проекте федерального закона № 500091-8", VoteStage.SecondReading, "500091-8")]
    [InlineData("(2 чтение) О проекте федерального закона № 500091-8", VoteStage.SecondReading, "500091-8")]
    [InlineData("КОАП РФ (глава 13) во 2 чт.", VoteStage.SecondReading, null)]
    [InlineData("ФЗ изм/доп в отдельные законодательные акты (2 чтение)", VoteStage.SecondReading, null)]
    [InlineData("(третье чтение) О проекте федерального закона № 500091-8", VoteStage.ThirdReading, "500091-8")]
    [InlineData("(3 чтение) О проекте федерального закона № 10962-5", VoteStage.ThirdReading, "10962-5")]
    [InlineData("(2 чтение таб. 1 принятые поправки) О проекте федерального закона № 500091-8", VoteStage.SecondReadingAcceptedAmendments, "500091-8")]
    [InlineData("(2 чтение таб. 2 отклонённые поправки) О проекте федерального закона № 500091-8", VoteStage.SecondReadingRejectedAmendments, "500091-8")]
    [InlineData("(2 чтение таб. 2 отклоненные поправки) О проекте федерального закона № 500091-8", VoteStage.SecondReadingRejectedAmendments, "500091-8")]
    [InlineData("табл 1 поправок к законопроекту", VoteStage.SecondReadingAcceptedAmendments, null)]
    [InlineData("(таблица 1) О проекте федерального закона № 10962-5", VoteStage.SecondReadingAcceptedAmendments, "10962-5")]
    [InlineData("табл.1 поправок к фз", VoteStage.SecondReadingAcceptedAmendments, null)]
    [InlineData("таб 1 прин попр", VoteStage.SecondReadingAcceptedAmendments, null)]
    [InlineData("табл 2 откл поправок", VoteStage.SecondReadingRejectedAmendments, null)]
    [InlineData("(таблица 2) О проекте федерального закона № 10962-5", VoteStage.SecondReadingRejectedAmendments, "10962-5")]
    [InlineData("таб 2 отклоненные поправки", VoteStage.SecondReadingRejectedAmendments, null)]
    [InlineData("ФЗ о внес изм в ФЗ о беженцах(табл 2 принятых поправок)", VoteStage.SecondReadingRejectedAmendments, null)]
    [InlineData("(2 чтение доп.таб. 2 отклоненные поправки) О проекте федерального закона № 10962-5", VoteStage.SecondReadingRejectedAmendments, "10962-5")]
    [InlineData("(за основу) О проекте федерального закона № 500091-8", VoteStage.AsBasis, "500091-8")]
    [InlineData("(в целом) О проекте федерального закона № 500091-8", VoteStage.AsWhole, "500091-8")]
    [InlineData("ст 10 в целом", VoteStage.AsWhole, null)]
    [InlineData("бюджет в 1 чтении за основу", VoteStage.FirstReading, null)]
    [InlineData("(к отклонению) О проекте федерального закона № 10962-5", VoteStage.Reject, "10962-5")]
    [InlineData("вернуть во 2 чтение законопроекта", VoteStage.ReturnToSecondReading, null)]
    [InlineData("вернуть во второе чтение законопроекта", VoteStage.ReturnToSecondReading, null)]
    [InlineData("(предл деп Зиятдиновой: включить ФЗ в повестку) О проекте порядка работы Государственной Думы на 28 ноября 2003 года", VoteStage.Procedural, null)]
    [InlineData("(за основу) О проекте порядка работы Государственной Думы на 27 июля 2026 года", VoteStage.Procedural, null)]
    [InlineData("повестка дня в целом", VoteStage.Procedural, null)]
    [InlineData("порядок работы за основу", VoteStage.Procedural, null)]
    [InlineData("Порядок Об избрании Председателя Государственной Думы Федерального Собрания Российской Федерации", VoteStage.Procedural, null)]
    [InlineData("процедура Об избрании Председателя Государственной Думы Федерального Собрания Российской Федерации", VoteStage.Procedural, null)]
    [InlineData("порядок обсуждения проекта федерального закона", VoteStage.Procedural, null)]
    [InlineData("Регламент в первом чтении", VoteStage.Procedural, null)]
    [InlineData("Перерыв до 16 часов завтра", VoteStage.Procedural, null)]
    [InlineData("Протокольное поручение депутата Рашкина, Обухова", VoteStage.Procedural, null)]
    [InlineData("О проекте постановления Государственной Думы \"Об изменениях в составе комитетов\"", VoteStage.Procedural, null)]
    [InlineData("О проекте постановления Государственной Думы № 6734-6 \"Об объявлении амнистии\"", VoteStage.Unknown, "6734-6")]
    [InlineData("Пост ГД о изм. в комитете по бюджету и налогам (повтор)", VoteStage.Procedural, null)]
    [InlineData("* (первое чтение) О проекте федерального закона № 10962-5", VoteStage.FirstReading, "10962-5")]
    [InlineData("*(2 чтение таб. 2 отклонённые поправки) О проекте федерального закона № 10962-5", VoteStage.SecondReadingRejectedAmendments, "10962-5")]
    [InlineData("** (третье чтение) О проекте федерального закона № 10962-5", VoteStage.ThirdReading, "10962-5")]
    [InlineData("О проекте федерального закона № 90040257-3 \"О ратификации соглашения\"", VoteStage.Unknown, "90040257-3")]
    [InlineData("предл деп Билалова: перейти к п 31", VoteStage.Procedural, null)]
    [InlineData("Вернуться к голосованию", VoteStage.Procedural, null)]
    [InlineData("Поправка 3(Зенкин)", VoteStage.Unknown, null)]
    [InlineData("ФЗ о внес изм", VoteStage.Unknown, null)]
    [InlineData("О проекте федерального закона №10962-5 \"О внесении изменений\"", VoteStage.Unknown, "10962-5")]
    [InlineData("(поправка №1 к постановлению) (первое чтение) О проекте федерального закона № 113667-7", VoteStage.FirstReading, "113667-7")]
    public void Normalize_StageAndBillNumber(string title, VoteStage expectedStage, string? expectedBillNumber)
    {
        var result = normalizer.Normalize(title);

        result.Stage.Should().Be(expectedStage);
        result.BillNumber.Should().Be(expectedBillNumber);
    }

    /// <summary>
    /// Пробелы схлопываются, включая неразрывные; края обрезаются
    /// </summary>
    [Fact]
    public void Normalize_Whitespace_CollapsedAndTrimmed()
    {
        var result = normalizer.Normalize("  (первое   чтение)   О проекте\u00A0закона  ");

        result.Title.Should().Be("(первое чтение) О проекте закона");
        result.Stage.Should().Be(VoteStage.FirstReading);
    }

    /// <summary>
    /// StageRaw для скобочной группы — текст группы без скобок
    /// </summary>
    [Fact]
    public void Normalize_StageRaw_GroupText()
    {
        var result = normalizer.Normalize("(2 чтение таб. 1 принятые поправки) О проекте федерального закона № 500091-8");

        result.StageRaw.Should().Be("2 чтение таб. 1 принятые поправки");
    }

    /// <summary>
    /// StageRaw для стадии без скобок — найденный фрагмент
    /// </summary>
    [Fact]
    public void Normalize_StageRaw_MatchedFragment()
    {
        var result = normalizer.Normalize("ст 10 в целом");

        result.StageRaw.Should().Be("в целом");
    }

    /// <summary>
    /// StageRaw неизвестной стадии — исходный заголовок
    /// </summary>
    [Fact]
    public void Normalize_StageRaw_UnknownTitle()
    {
        var result = normalizer.Normalize("Поправка 3(Зенкин)");

        result.Stage.Should().Be(VoteStage.Unknown);
        result.StageRaw.Should().Be("Поправка 3(Зенкин)");
    }

    /// <summary>
    /// StageRaw процедурной темы с ведущей группой — текст группы
    /// </summary>
    [Fact]
    public void Normalize_StageRaw_ProceduralLeadGroup()
    {
        var result = normalizer.Normalize("(за основу) О проекте порядка работы Государственной Думы на 27 июля 2026 года");

        result.Stage.Should().Be(VoteStage.Procedural);
        result.StageRaw.Should().Be("за основу");
    }

    /// <summary>
    /// Длинный заголовок неизвестной стадии: StageRaw обрезается до константы
    /// </summary>
    [Fact]
    public void Normalize_LongUnknownTitle_StageRawTruncated()
    {
        var result = normalizer.Normalize(new string('а', 600));

        result.Stage.Should().Be(VoteStage.Unknown);
        result.StageRaw.Should().NotBeNull();
        result.StageRaw!.Length.Should().Be(VoteValidationConstants.StageRawMaxLength);
    }

    /// <summary>
    /// Выборка из 24 реальных заголовков архива (по 3 на созывы I–VIII):
    /// нормализатор не падает, очищает текст, StageRaw заполнен, номер законопроекта корректен
    /// </summary>
    [Fact]
    public void Normalize_RealArchiveSample_AllConvocations()
    {
        var sample = JsonSerializer.Deserialize<List<SampleTitle>>(
            FixtureFiles.Json("archive_titles_sample.json"))!;

        sample.Should().HaveCount(24);
        sample.Select(s => s.Convocation).Distinct().Should().BeEquivalentTo(Enumerable.Range(1, 8));

        foreach (var item in sample)
        {
            var result = normalizer.Normalize(item.Title);

            result.Title.Should().NotBeNullOrWhiteSpace();
            result.Title.Should().NotContain("  ");
            result.Title.Should().Be(result.Title.Trim());
            result.StageRaw.Should().NotBeNullOrEmpty();
            result.Stage.Should().BeDefined();
            if (result.BillNumber is not null)
            {
                result.BillNumber.Should().MatchRegex(@"^\d{1,10}-\d{1,2}$");
            }
        }

        var byId = sample.ToDictionary(s => s.SiteId);
        normalizer.Normalize(byId[10].Title).Stage.Should().Be(VoteStage.Procedural);
        normalizer.Normalize(byId[18643].Title).Stage.Should().Be(VoteStage.AsBasis);
        normalizer.Normalize(byId[51852].Title).Stage.Should().Be(VoteStage.Procedural);
        normalizer.Normalize(byId[75790].Title).Stage.Should().Be(VoteStage.Procedural);
        normalizer.Normalize(byId[96014].Title).Stage.Should().Be(VoteStage.Procedural);
        normalizer.Normalize(byId[115587].Title).Stage.Should().Be(VoteStage.Procedural);

        var resolution = normalizer.Normalize(byId[75786].Title);
        resolution.Stage.Should().Be(VoteStage.Unknown);
        resolution.BillNumber.Should().Be("646240-5");
    }

    /// <summary>
    /// Элемент выборки реальных заголовков архива
    /// </summary>
    private sealed class SampleTitle
    {
        /// <summary>
        /// Номер созыва
        /// </summary>
        [JsonPropertyName("convocation")]
        public int Convocation { get; set; }

        /// <summary>
        /// Идентификатор голосования на сайте
        /// </summary>
        [JsonPropertyName("siteId")]
        public long SiteId { get; set; }

        /// <summary>
        /// Исходный заголовок
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// Пустой заголовок: Unknown, пустые поля
    /// </summary>
    [Fact]
    public void Normalize_EmptyTitle_Unknown()
    {
        var result = normalizer.Normalize(string.Empty);

        result.Title.Should().BeEmpty();
        result.Stage.Should().Be(VoteStage.Unknown);
        result.BillNumber.Should().BeNull();
    }
}