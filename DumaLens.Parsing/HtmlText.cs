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
    /// <remarks>
    /// Быстрый путь без аллокаций: если пробелы уже одиночные и края чистые, возвращается исходная строка
    /// (подавляющее большинство заголовков и ФИО архива).
    /// </remarks>
    public static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return NeedsWhitespaceCleanup(value)
            ? WhitespaceRegex().Replace(value, " ").Trim()
            : value;
    }

    /// <summary>
    /// Очищает ФИО депутата: дополнительно разделяет склеенные слова на переходе
    /// строчная→заглавная кириллица («АбакаровХизри» → «Абакаров Хизри»)
    /// </summary>
    public static string CleanFullName(string? value)
    {
        var cleaned = Clean(value);
        return NeedsGlueSplit(cleaned)
            ? GlueRegex().Replace(cleaned, " ")
            : cleaned;
    }

    private static bool NeedsWhitespaceCleanup(string value)
    {
        var previousWasSpace = true;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsWhiteSpace(c))
            {
                if (previousWasSpace || c != ' ')
                {
                    return true;
                }

                previousWasSpace = true;
            }
            else
            {
                previousWasSpace = false;
            }
        }

        return previousWasSpace;
    }

    private static bool NeedsGlueSplit(string value)
    {
        for (var i = 1; i < value.Length; i++)
        {
            if (IsLowerCyrillic(value[i - 1]) && IsUpperCyrillic(value[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLowerCyrillic(char c)
    {
        return (c >= 'а' && c <= 'я') || c == 'ё';
    }

    private static bool IsUpperCyrillic(char c)
    {
        return (c >= 'А' && c <= 'Я') || c == 'Ё';
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?<=[а-яё])(?=[А-ЯЁ])")]
    private static partial Regex GlueRegex();
}