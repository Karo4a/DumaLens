using System.Text.RegularExpressions;

namespace DumaLens.Repositories.Search;

/// <summary>
/// Строит текст tsquery из поисковой строки пользователя для конфигурации russian
/// </summary>
/// <remarks>
/// Синтаксис — подмножество веб-поиска: слова через пробел = И, "фраза" = соседство слов,
/// -слово = НЕ, OR или | = ИЛИ. Каждая лексема получает префикс :* — to_tsquery('russian', 'алкоголь:*')
/// даёт 'алкогол':* и находит и алкогол (алкоголя), и алкогольн (алкогольной). websearch_to_tsquery
/// этого не делает: прилагательное и существительное — разные лексемы, и поиск «алкоголь» не находил
/// законопроект 500091-8 (П-4): на полном архиве 2 совпадения против 530 у префиксного поиска.
/// Морфологию выполняет PostgreSQL (to_tsquery с конфигурацией russian), построитель только
/// расставляет операторы и очищает ввод от служебных символов tsquery.
/// </remarks>
public static class RussianTsQueryBuilder
{
    private static readonly Regex WordRuns = new(@"[\p{L}\p{N}-]+", RegexOptions.Compiled);
    private static readonly Regex LetterOrDigit = new(@"[\p{L}\p{N}]", RegexOptions.Compiled);

    /// <summary>
    /// Разбирает поисковую строку в текст tsquery
    /// </summary>
    /// <param name="query">Поисковая строка пользователя</param>
    /// <returns>
    /// Текст tsquery для to_tsquery('russian', …); null — пустой ввод (FTS-фильтр не применяется);
    /// пустая строка — во вводе нет лексем (только стоп-слова/символы), совпадений не будет
    /// </returns>
    public static string? Build(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }
        var segments = new List<List<string>>();
        var current = new List<string>();
        foreach (var token in Tokenize(query))
        {
            if (token.IsOr)
            {
                if (current.Count > 0)
                {
                    segments.Add(current);
                    current = [];
                }
                continue;
            }
            var term = BuildTerm(token);
            if (term != null)
            {
                current.Add(term);
            }
        }
        if (current.Count > 0)
        {
            segments.Add(current);
        }
        return string.Join(" | ", segments.Select(segment => string.Join(" & ", segment)));
    }

    /// <summary>
    /// Разбивает строку на токены: слова, фразы в кавычках, минус-слова, разделители ИЛИ
    /// </summary>
    private static List<Token> Tokenize(string query)
    {
        var tokens = new List<Token>();
        var index = 0;
        while (index < query.Length)
        {
            var symbol = query[index];
            if (char.IsWhiteSpace(symbol))
            {
                index++;
                continue;
            }
            if (symbol == '|')
            {
                tokens.Add(Token.Or());
                index++;
                continue;
            }
            var negated = false;
            if (symbol == '-')
            {
                negated = true;
                index++;
            }
            if (index < query.Length && query[index] == '"')
            {
                index++;
                var start = index;
                while (index < query.Length && query[index] != '"')
                {
                    index++;
                }
                var phrase = query[start..index];
                if (index < query.Length)
                {
                    index++;
                }
                tokens.Add(new Token(phrase, IsPhrase: true, negated, IsOr: false));
                continue;
            }
            var wordStart = index;
            while (index < query.Length && !char.IsWhiteSpace(query[index]) && query[index] != '"' && query[index] != '|')
            {
                index++;
            }
            var word = query[wordStart..index];
            if (word.Equals("OR", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Add(Token.Or());
                continue;
            }
            tokens.Add(new Token(word, IsPhrase: false, negated, IsOr: false));
        }
        return tokens;
    }

    /// <summary>
    /// Собирает терм tsquery из токена: лексемы с префиксом :*, фраза — соседство, минус — отрицание
    /// </summary>
    private static string? BuildTerm(Token token)
    {
        var lexemes = WordRuns.Matches(token.Text)
            .Select(match => match.Value)
            .Where(value => LetterOrDigit.IsMatch(value))
            .Select(value => value + ":*")
            .ToList();
        if (lexemes.Count == 0)
        {
            return null;
        }
        var term = lexemes.Count == 1
            ? lexemes[0]
            : "(" + string.Join(token.IsPhrase ? " <-> " : " & ", lexemes) + ")";
        return token.Negated ? "!" + term : term;
    }

    /// <summary>
    /// Токен поисковой строки
    /// </summary>
    /// <param name="Text">Слово или содержимое кавычек</param>
    /// <param name="IsPhrase">Фраза в кавычках (соседство лексем)</param>
    /// <param name="Negated">Минус-слово (отрицание)</param>
    /// <param name="IsOr">Разделитель ИЛИ</param>
    private sealed record Token(string Text, bool IsPhrase, bool Negated, bool IsOr)
    {
        /// <summary>
        /// Разделитель ИЛИ
        /// </summary>
        public static Token Or() => new Token(string.Empty, IsPhrase: false, Negated: false, IsOr: true);
    }
}