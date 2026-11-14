using System.Text.RegularExpressions;

namespace DumaLens.Parsing;

/// <summary>
/// Очистка текста заголовков и ФИО: схлопывание пробелов (включая неразрывные) и обрезка краёв
/// </summary>
/// <remarks>
/// HTML-сущности декодируются AngleSharp при чтении TextContent; JSON архива содержит обычный текст.
/// </remarks>
internal static partial class HtmlText
{
    /// <summary>
    /// Очищает текст: все последовательности пробельных символов заменяются одним пробелом
    /// </summary>
    public static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return WhitespaceRegex().Replace(value, " ").Trim();
    }

    /// <summary>
    /// Очищает ФИО депутата: дополнительно разделяет склеенные слова на переходе
    /// строчная→заглавная кириллица («АбакаровХизри» → «Абакаров Хизри»)
    /// </summary>
    public static string CleanFullName(string? value)
    {
        return GlueRegex().Replace(Clean(value), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?<=[а-яё])(?=[А-ЯЁ])")]
    private static partial Regex GlueRegex();
}