using DumaLens.Api.Client;
using System.Globalization;

namespace DumaLens.Web.Components.Shared;

/// <summary>
/// Форматирование значений и русские подписи enumerations интерфейса
/// </summary>
public static class UiFormat
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>
    /// Канонический порядок фракций: ЕР, КПРФ, ЛДПР, СР, НЛ, независимые; вне фракций — последние
    /// </summary>
    private static readonly string[] FactionOrder = ["ER", "KPRF", "LDPR", "SR", "NL", "INDEPENDENT"];

    /// <summary>
    /// Процент в русском формате (запятая, до 2 знаков); null — прочерк
    /// </summary>
    public static string Percent(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.##", Ru) + "%" : "—";
    }

    /// <summary>
    /// Целое число с разделителем тысяч (русский формат)
    /// </summary>
    public static string Number(int value)
    {
        return value.ToString("N0", Ru);
    }

    /// <summary>
    /// Склонение существительного после числа: 1 голосование, 2 голосования, 5 голосований
    /// </summary>
    public static string Plural(int count, string one, string few, string many)
    {
        var n = Math.Abs(count) % 100;
        if (n >= 11 && n <= 14)
        {
            return many;
        }

        var lastDigit = n % 10;
        return lastDigit switch
        {
            1 => one,
            2 or 3 or 4 => few,
            _ => many,
        };
    }

    /// <summary>
    /// Дата в формате ДД.ММ.ГГГГ
    /// </summary>
    public static string Date(DateTimeOffset value)
    {
        return value.ToString("dd.MM.yyyy", Ru);
    }

    /// <summary>
    /// Дата и время в формате ДД.ММ.ГГГГ ЧЧ:ММ
    /// </summary>
    public static string DateTimeLabel(DateTimeOffset value)
    {
        return value.ToString("dd.MM.yyyy HH:mm", Ru);
    }

    /// <summary>
    /// Диапазон дат для периода (открытая граница — «н. в.»)
    /// </summary>
    public static string DateRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        var left = from.HasValue ? Date(from.Value) : "…";
        var right = to.HasValue ? Date(to.Value) : "н. в.";
        return left + " — " + right;
    }

    /// <summary>
    /// Русская подпись стадии рассмотрения
    /// </summary>
    public static string StageLabel(VoteStage stage)
    {
        return stage switch
        {
            VoteStage.FirstReading => "первое чтение",
            VoteStage.SecondReading => "второе чтение",
            VoteStage.SecondReadingAcceptedAmendments => "поправки (принятые)",
            VoteStage.SecondReadingRejectedAmendments => "поправки (отклонённые)",
            VoteStage.ThirdReading => "третье чтение",
            VoteStage.AsBasis => "за основу",
            VoteStage.AsWhole => "в целом",
            VoteStage.Reject => "отклонение законопроекта",
            VoteStage.ReturnToSecondReading => "возврат ко второму чтению",
            VoteStage.Procedural => "процедурное",
            _ => "неизвестно",
        };
    }

    /// <summary>
    /// Русская подпись итога голосования
    /// </summary>
    public static string OutcomeLabel(VoteOutcome outcome)
    {
        return outcome switch
        {
            VoteOutcome.Accepted => "Принят",
            VoteOutcome.Rejected => "Отклонён",
            _ => "Неизвестно",
        };
    }

    /// <summary>
    /// CSS-класс итога голосования (семантика цвета результата)
    /// </summary>
    public static string OutcomeClass(VoteOutcome outcome)
    {
        return outcome switch
        {
            VoteOutcome.Accepted => "r-for",
            VoteOutcome.Rejected => "r-against",
            _ => "r-none",
        };
    }

    /// <summary>
    /// Русская подпись результата голоса депутата
    /// </summary>
    public static string ResultLabel(VoteResult result)
    {
        return result switch
        {
            VoteResult.Accept => "ЗА",
            VoteResult.Decline => "ПРОТИВ",
            VoteResult.Abstain => "ВОЗД",
            _ => "НЕ ГОЛ",
        };
    }

    /// <summary>
    /// Короткая метка результата (одна буква для маркера депутата)
    /// </summary>
    public static string ResultMark(VoteResult result)
    {
        return result switch
        {
            VoteResult.Accept => "З",
            VoteResult.Decline => "П",
            VoteResult.Abstain => "В",
            _ => "Н",
        };
    }

    /// <summary>
    /// CSS-класс результата голоса (семантика цвета)
    /// </summary>
    public static string ResultClass(VoteResult result)
    {
        return result switch
        {
            VoteResult.Accept => "r-for",
            VoteResult.Decline => "r-against",
            VoteResult.Abstain => "r-abstain",
            _ => "r-none",
        };
    }

    /// <summary>
    /// Русская подпись источника данных голосования
    /// </summary>
    public static string SourceLabel(VoteSource source)
    {
        return source switch
        {
            VoteSource.Archive => "архив",
            _ => "сайт",
        };
    }

    /// <summary>
    /// Ключ сортировки фракций в каноническом порядке (вне фракций — последние)
    /// </summary>
    public static int FactionSortKey(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return 100;
        }

        var index = Array.IndexOf(FactionOrder, code);
        return index >= 0 ? index : 50;
    }
}