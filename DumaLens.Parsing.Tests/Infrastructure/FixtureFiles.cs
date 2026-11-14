namespace DumaLens.Parsing.Tests.Infrastructure;

/// <summary>
/// Загрузка реальных фикстур (HTML сайта vote.duma.gov.ru и JSON архива vote-master.zip)
/// </summary>
internal static class FixtureFiles
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    /// <summary>
    /// Читает HTML-фикстуру по имени файла
    /// </summary>
    public static string Html(string name)
    {
        return File.ReadAllText(Path.Combine(Root, "html", name));
    }

    /// <summary>
    /// Читает JSON-фикстуру архива по имени файла
    /// </summary>
    public static string Json(string name)
    {
        return File.ReadAllText(Path.Combine(Root, "json", name));
    }
}