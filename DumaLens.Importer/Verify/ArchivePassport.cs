using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Importer.Verify;

/// <summary>
/// Паспорт архива vote-master: контрольные суммы для сверки после импорта
/// (Docs/02-data-sources.md, §2.2; коммит архива b9f6118)
/// </summary>
public static class ArchivePassport
{
    /// <summary>
    /// Всего голосований в архиве
    /// </summary>
    public const long TotalVotes = 102_320;

    /// <summary>
    /// Всего индивидуальных голосов депутатов
    /// </summary>
    public const long TotalDeputyVotes = 45_593_720;

    /// <summary>
    /// Голосований без результатов (votes = [], факт, не аномалия)
    /// </summary>
    public const long EmptyVotes = 78;

    /// <summary>
    /// Уникальных номеров законопроектов в заголовках голосований (широкий шаблон нормализатора
    /// «№\s*(\d{1,10}-\d{1,2})»; полный проход архива 14.11.2026). Прежнее значение паспорта
    /// 17 608 устарело: узкий шаблон «№\s*(\d{4,7}-\d)» даёт 20 582, широкий — 20 932
    /// </summary>
    public const long UniqueBillNumbers = 20_932;

    /// <summary>
    /// Контрольные суммы по созывам (номер созыва → суммы)
    /// </summary>
    public static IReadOnlyDictionary<int, ConvocationChecksum> Convocations { get; } = new Dictionary<int, ConvocationChecksum>
    {
        [1] = new ConvocationChecksum(7_088, 3_149_588),
        [2] = new ConvocationChecksum(14_870, 6_592_078),
        [3] = new ConvocationChecksum(15_223, 6_721_523),
        [4] = new ConvocationChecksum(11_853, 5_280_681),
        [5] = new ConvocationChecksum(12_425, 5_576_548),
        [6] = new ConvocationChecksum(15_767, 7_063_368),
        [7] = new ConvocationChecksum(16_304, 7_271_288),
        [8] = new ConvocationChecksum(8_790, 3_938_646),
    };

    /// <summary>
    /// Суммарные коды результатов голосования депутатов
    /// </summary>
    public static IReadOnlyDictionary<VoteResult, long> ResultTotals { get; } = new Dictionary<VoteResult, long>
    {
        [VoteResult.Accept] = 27_005_267,
        [VoteResult.Decline] = 1_912_955,
        [VoteResult.Abstain] = 122_909,
        [VoteResult.None] = 16_552_589,
    };
}