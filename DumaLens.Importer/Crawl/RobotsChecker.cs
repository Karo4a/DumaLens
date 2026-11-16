using DumaLens.Importer.Import;

namespace DumaLens.Importer.Crawl;

/// <summary>
/// Проверка robots.txt сайта перед обходом (Э7): группы User-agent «*» и «dumalens»
/// </summary>
/// <remarks>
/// Отсутствующий или недоступный robots.txt трактуется как «разрешено всё» (стандартная
/// семантика). Префиксы Disallow проверяются ordinal-сравнением по пути запроса.
/// </remarks>
public sealed class RobotsChecker
{
    private readonly HttpClient httpClient;
    private readonly ImportLog log;
    private readonly List<string> disallowedPrefixes = [];

    /// <summary>
    /// Конструктор
    /// </summary>
    public RobotsChecker(HttpClient httpClient, ImportLog log)
    {
        this.httpClient = httpClient;
        this.log = log;
    }

    /// <summary>
    /// Загрузить и разобрать robots.txt базового URL
    /// </summary>
    public async Task LoadAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var url = baseUrl.TrimEnd('/') + "/robots.txt";
        string? text = null;
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                text = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            else
            {
                log.Info($"robots.txt вернул HTTP {(int)response.StatusCode} — обход разрешён по умолчанию");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            log.Info($"robots.txt недоступен ({ex.GetType().Name}) — обход разрешён по умолчанию");
        }

        if (text is null)
        {
            return;
        }

        var appliesToUs = false;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            var commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                line = line[..commentIndex].Trim();
            }

            if (line.Length == 0)
            {
                continue;
            }

            var separator = line.IndexOf(':');
            if (separator < 0)
            {
                continue;
            }

            var field = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (field.Equals("User-agent", StringComparison.OrdinalIgnoreCase))
            {
                appliesToUs = value == "*" || value.Contains("dumalens", StringComparison.OrdinalIgnoreCase);
            }
            else if (field.Equals("Disallow", StringComparison.OrdinalIgnoreCase) && appliesToUs && value.Length > 0)
            {
                disallowedPrefixes.Add(value);
            }
        }

        log.Info($"robots.txt разобран: запрещённых префиксов для кроулера {disallowedPrefixes.Count}");
    }

    /// <summary>
    /// Разрешён ли путь запроса по разобранным правилам
    /// </summary>
    public bool IsAllowed(string path)
    {
        return !disallowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
    }
}