using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.Contracts.Enums;

namespace DumaLens.Services.Search;

/// <summary>
/// Разбор строковых токенов контракта поиска в перечисления (регистронезависимо, только имена)
/// </summary>
/// <remarks>
/// Числовые значения перечислений не принимаются: контракт API оперирует именами (ThirdReading, accept, gte).
/// Возврат null означает неизвестный токен; валидатор и сервис используют одни и те же методы.
/// </remarks>
internal static class SearchTokenParser
{
    /// <summary>
    /// Разбирает стадию голосования (канонические значения VoteStage, 04-functionality.md §2.2)
    /// </summary>
    internal static VoteStage? ParseStage(string? token)
        => ParseEnum<VoteStage>(token);

    /// <summary>
    /// Разбирает исход голосования (accepted/rejected/unknown)
    /// </summary>
    internal static VoteOutcome? ParseOutcome(string? token)
        => ParseEnum<VoteOutcome>(token);

    /// <summary>
    /// Разбирает результат фракции (accept/decline/abstain/none)
    /// </summary>
    internal static VoteResult? ParseResult(string? token)
        => ParseEnum<VoteResult>(token);

    /// <summary>
    /// Разбирает оператор сравнения (gte/lte)
    /// </summary>
    internal static ComparisonOperator? ParseOperator(string? token)
        => ParseEnum<ComparisonOperator>(token);

    /// <summary>
    /// Разбирает базу процента (members/voted)
    /// </summary>
    internal static PercentBasis? ParseBasis(string? token)
        => ParseEnum<PercentBasis>(token);

    /// <summary>
    /// Разбирает сортировку (dateDesc/dateAsc/relevance/acceptCountDesc)
    /// </summary>
    internal static VoteSearchSort? ParseSort(string? token)
        => ParseEnum<VoteSearchSort>(token);

    /// <summary>
    /// Ищет имя перечисления без учёта регистра; числовые строки отвергаются
    /// </summary>
    private static TEnum? ParseEnum<TEnum>(string? token)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }
        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, token, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<TEnum>(name);
            }
        }
        return null;
    }
}