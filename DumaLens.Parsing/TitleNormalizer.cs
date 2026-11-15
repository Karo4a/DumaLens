using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Parsing.Contracts;
using DumaLens.Parsing.Contracts.Models;
using System.Text.RegularExpressions;

namespace DumaLens.Parsing;

/// <summary>
/// Нормализатор заголовка: очистка, номер законопроекта, стадия по словарю (04-functionality.md, §2.2)
/// </summary>
/// <remarks>
/// Словарь расширен по фактическим заголовкам архива (полный проход 102 320 заголовков):
/// ведущие маркеры «*»/«**», стадии в скобках и без, свободные заголовки I–III созывов
/// («ст 10 в целом», «табл 2 откл поправок», «1-чт»). При конфликте приоритет более специфичного
/// варианта: таблица поправок/возврат &gt; чтение &gt; «за основу»/«в целом». Таблицы и возврат
/// выигрывают у чтений независимо от позиции; среди чтений и «к отклонению» стадию задаёт ведущая
/// группа (хвостовые уточнения её не перебивают); «за основу»/«в целом» учитываются, только если
/// чтение/отклонение не найдены. Исторические справки («принят в 1 чтении 21.11.08 …») стадией
/// не считаются и исключаются из кандидатов; процедурная тема (порядок работы, повестка,
/// предложения депутатов) приоритетнее способа принятия.
/// StageRaw обрезается до <see cref="VoteValidationConstants.StageRawMaxLength"/> на всех путях
/// (скобочная группа, ведущая группа, фрагмент совпадения, исходный заголовок).
/// Номер законопроекта: 1–10 цифр (в архиве встречаются 8-значные номера старых созывов,
/// например 90040257-3) и 1–2 цифры после дефиса; «№» без пробела встречается в 1 643 заголовках.
/// </remarks>
public sealed partial class TitleNormalizer : ITitleNormalizer
{
    /// <inheritdoc/>
    public NormalizedTitle Normalize(string title)
    {
        var cleaned = HtmlText.Clean(title);
        var result = new NormalizedTitle { Title = cleaned };

        var numberMatch = BillNumberRegex().Match(cleaned);
        if (numberMatch.Success)
        {
            result.BillNumber = numberMatch.Groups[1].Value;
        }

        var core = cleaned.ToLowerInvariant().Replace('ё', 'е').TrimStart('*').TrimStart();
        var groups = ParenGroupRegex().Matches(core).Select(m => m.Groups[1].Value.Trim()).ToList();
        var leadGroup = core.StartsWith('(') && groups.Count > 0 ? groups[0] : null;

        // Исторические справки («принят в 1 чтении 21.11.08 …») описывают прошлое законопроекта,
        // а не стадию текущего голосования, и не участвуют в определении стадии
        var stageGroups = groups.Where(group => !HistoricalNoteRegex().IsMatch(group)).ToList();
        var matchCore = ParenGroupRegex().Replace(
            core,
            match => HistoricalNoteRegex().IsMatch(match.Groups[1].Value.Trim()) ? string.Empty : match.Value);
        matchCore = HtmlText.Clean(matchCore);
        var subject = StripLeadingGroups(matchCore);

        if (MatchesAny(ProceduralPrefixPatterns, subject))
        {
            result.Stage = VoteStage.Procedural;
            result.StageRaw = Truncate(leadGroup ?? cleaned);
            return result;
        }

        var candidates = new List<(string Text, bool IsGroup)>();
        foreach (var group in stageGroups)
        {
            candidates.Add((group, true));
        }

        candidates.Add((subject, false));
        candidates.Add((matchCore, false));

        if (TryMatchStage(result, SpecificStagePatterns, candidates, patternPriority: true)
            || TryMatchStage(result, ReadingStagePatterns, candidates, patternPriority: false)
            || TryMatchStage(result, MannerStagePatterns, candidates, patternPriority: false))
        {
            return result;
        }

        if (result.BillNumber is null && MatchesAny(ResolutionPrefixPatterns, subject))
        {
            result.Stage = VoteStage.Procedural;
            result.StageRaw = Truncate(leadGroup ?? cleaned);
            return result;
        }

        result.Stage = VoteStage.Unknown;
        result.StageRaw = Truncate(leadGroup ?? cleaned);
        return result;
    }

    /// <summary>
    /// Убирает ведущие скобочные группы («(за основу) (первое чтение) …» → остаток заголовка)
    /// </summary>
    private static string StripLeadingGroups(string core)
    {
        var rest = core;
        while (rest.StartsWith('('))
        {
            var close = rest.IndexOf(')');
            if (close < 0)
            {
                break;
            }

            rest = rest[(close + 1)..].TrimStart();
        }

        return rest;
    }

    /// <summary>
    /// Подбирает стадию по кандидатам: patternPriority задаёт приоритет паттерна над позицией
    /// (таблицы поправок, возврат), иначе ведущий кандидат важнее хвостовых (чтения, способ принятия)
    /// </summary>
    private static bool TryMatchStage(
        NormalizedTitle result,
        (VoteStage Stage, Regex Pattern)[] patterns,
        List<(string Text, bool IsGroup)> candidates,
        bool patternPriority)
    {
        var pairs = patternPriority
            ? patterns.SelectMany(pattern => candidates.Select(candidate => (pattern.Stage, pattern.Pattern, candidate.Text, candidate.IsGroup)))
            : candidates.SelectMany(candidate => patterns.Select(pattern => (pattern.Stage, pattern.Pattern, candidate.Text, candidate.IsGroup)));

        foreach (var (stage, pattern, text, isGroup) in pairs)
        {
            var match = pattern.Match(text);
            if (!match.Success)
            {
                continue;
            }

            result.Stage = stage;
            result.StageRaw = Truncate(isGroup ? text : match.Value);
            return true;
        }

        return false;
    }

    private static bool MatchesAny(Regex[] patterns, string text)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.IsMatch(text))
            {
                return true;
            }
        }

        return false;
    }

    private static string Truncate(string value)
    {
        return value.Length <= VoteValidationConstants.StageRawMaxLength
            ? value
            : value[..VoteValidationConstants.StageRawMaxLength];
    }

    [GeneratedRegex(@"№\s*(\d{1,10}-\d{1,2})(?!\d)")]
    private static partial Regex BillNumberRegex();

    [GeneratedRegex(@"\(([^()]*)\)")]
    private static partial Regex ParenGroupRegex();

    [GeneratedRegex(@"^(?:принят|одобрен)\s+(?:в|во)\s")]
    private static partial Regex HistoricalNoteRegex();

    /// <summary>
    /// Специфичные стадии: возврат и таблицы поправок; приоритет паттерна над позицией в заголовке
    /// («(второе чтение) (2 чтение таб. 1 …)» — голосование по таблице, а не по чтению в целом)
    /// </summary>
    private static readonly (VoteStage Stage, Regex Pattern)[] SpecificStagePatterns =
    [
        (VoteStage.ReturnToSecondReading, new Regex(@"вернуть\s+(?:во|в)\s+(?:2|второе)\s+чтение", RegexOptions.Compiled)),
        (VoteStage.SecondReadingAcceptedAmendments, new Regex(@"(?<![а-яё])таб\w*\.?\s*1(?!\d)", RegexOptions.Compiled)),
        (VoteStage.SecondReadingRejectedAmendments, new Regex(@"(?<![а-яё])таб\w*\.?\s*2(?!\d)", RegexOptions.Compiled))
    ];

    /// <summary>
    /// Чтения и «к отклонению»: стадию задаёт ведущая группа, хвостовые не перебивают её
    /// («(к отклонению) (третье чтение)» — отклонение; «(3 чтение) … (второе чтение)» — третье чтение)
    /// </summary>
    private static readonly (VoteStage Stage, Regex Pattern)[] ReadingStagePatterns =
    [
        (VoteStage.FirstReading, new Regex(@"(?<!\d)(?:перв(?:ое|ом)|1)\s*-?\s*чт", RegexOptions.Compiled)),
        (VoteStage.SecondReading, new Regex(@"(?<!\d)(?:втор(?:ое|ом)|2)\s*-?\s*чт", RegexOptions.Compiled)),
        (VoteStage.ThirdReading, new Regex(@"(?<!\d)(?:треть(?:е|ем)|3)\s*-?\s*чт", RegexOptions.Compiled)),
        (VoteStage.Reject, new Regex(@"к\s+отклонению", RegexOptions.Compiled))
    ];

    /// <summary>
    /// Способ принятия («за основу», «в целом») — только если чтение/отклонение не найдены
    /// («(за основу) (первое чтение)» — первое чтение)
    /// </summary>
    private static readonly (VoteStage Stage, Regex Pattern)[] MannerStagePatterns =
    [
        (VoteStage.AsBasis, new Regex(@"за\s+основу", RegexOptions.Compiled)),
        (VoteStage.AsWhole, new Regex(@"в\s+целом", RegexOptions.Compiled))
    ];

    /// <summary>
    /// Процедурные темы: совпадение в начале темы заголовка (после ведущих скобочных групп)
    /// </summary>
    private static readonly Regex[] ProceduralPrefixPatterns =
    [
        new Regex(@"^(?:о\s+проекте\s+)?поряд\w*\s+(?:работ|обсужден|об\s+избран)", RegexOptions.Compiled),
        new Regex(@"^процедур", RegexOptions.Compiled),
        new Regex(@"^повестк", RegexOptions.Compiled),
        new Regex(@"^предл\w*\.?\s+деп", RegexOptions.Compiled),
        new Regex(@"^пред\s+деп", RegexOptions.Compiled),
        new Regex(@"^протокольн\w+\s+поручен", RegexOptions.Compiled),
        new Regex(@"^прени", RegexOptions.Compiled),
        new Regex(@"^перерыв", RegexOptions.Compiled),
        new Regex(@"^продли", RegexOptions.Compiled),
        new Regex(@"^прекрати", RegexOptions.Compiled),
        new Regex(@"^включи\w*\s+(?:фз\s+)?в\s+повестк", RegexOptions.Compiled),
        new Regex(@"^вкл\w*\.?\s+в\s+повестк", RegexOptions.Compiled),
        new Regex(@"^создать\s+согласительн", RegexOptions.Compiled),
        new Regex(@"^снять\s+с\s+дальнейш", RegexOptions.Compiled),
        new Regex(@"^вернуться\s+к\s+(?:голосован|переголосован)", RegexOptions.Compiled),
        new Regex(@"^календарн\w+\s+план", RegexOptions.Compiled),
        new Regex(@"^о\s+проекте\s+календаря", RegexOptions.Compiled),
        new Regex(@"^регламент", RegexOptions.Compiled)
    ];

    /// <summary>
    /// Постановления без номера законопроекта — процедурные голосования (§2.2)
    /// </summary>
    private static readonly Regex[] ResolutionPrefixPatterns =
    [
        new Regex(@"^о\s+проекте\s+постановления", RegexOptions.Compiled),
        new Regex(@"^проект\s+постановления", RegexOptions.Compiled),
        new Regex(@"^пост\s+гд", RegexOptions.Compiled),
        new Regex(@"^постановление\s+гд", RegexOptions.Compiled)
    ];
}