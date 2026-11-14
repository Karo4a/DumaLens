using DumaLens.Entities;

namespace DumaLens.Importer.Import;

/// <summary>
/// Сопоставляет названия фракций из архива каноническим фракциям справочника
/// </summary>
public sealed class FactionCanonResolver
{
    private static readonly Dictionary<string, string> TitleAliases = new(StringComparer.Ordinal)
    {
        ["ЕР"] = "ER",
        ["Е-ЕР"] = "ER",
        ["О-ЕР"] = "ER",
        ["СР"] = "SR",
        ["СР-Р"] = "SR",
        ["НЛ"] = "NL",
    };

    private readonly IReadOnlyDictionary<string, Faction> factionsByTitle;
    private readonly IReadOnlyDictionary<string, Faction> factionsByCode;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="factionsByTitle">Канонические фракции по названию</param>
    /// <param name="factionsByCode">Канонические фракции по коду</param>
    public FactionCanonResolver(IReadOnlyDictionary<string, Faction> factionsByTitle, IReadOnlyDictionary<string, Faction> factionsByCode)
    {
        this.factionsByTitle = factionsByTitle;
        this.factionsByCode = factionsByCode;
    }

    /// <summary>
    /// Найти каноническую фракцию по названию из архива; null — название неизвестно
    /// </summary>
    public Faction? Resolve(string title)
    {
        if (factionsByTitle.TryGetValue(title, out var byTitle))
        {
            return byTitle;
        }

        if (TitleAliases.TryGetValue(title, out var code) && factionsByCode.TryGetValue(code, out var byCode))
        {
            return byCode;
        }

        return null;
    }
}