using DumaLens.Entities;

namespace DumaLens.Entities.Default;

/// <summary>
/// Стандартные канонические фракции
/// </summary>
/// <remarks>
/// Современные фракции и исторические фракции I–IV созывов (паспорт данных, 02-data-sources.md, §2.3.6).
/// Исторические периоды («Е-ЕР», «О-ЕР», «СР-Р» и т.п.) связываются с каноном в <see cref="FactionPeriodsDefault"/>.
/// Полный справочник достраивается импортом; идентификаторы фиксированы для HasData.
/// </remarks>
public class FactionsDefault
{
    /// <summary>
    /// Сущности стандартных фракций
    /// </summary>
    public static List<Faction> Entities = new List<Faction>()
    {
        new Faction()
        {
            Id = Guid.Parse("f0e605dc-c9c2-4894-8c83-13bf0480e62d"),
            Code = "ER",
            Title = "Единая Россия",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("6699bf12-62aa-4157-8ce5-ba600cc9b3a1"),
            Code = "KPRF",
            Title = "КПРФ",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("b5265678-a153-4ade-9010-022275aabac6"),
            Code = "LDPR",
            Title = "ЛДПР",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("51cf331e-c029-4e16-9eec-affba832201e"),
            Code = "SR",
            Title = "Справедливая Россия",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("92d7276d-bea2-4751-a4c1-7f8c0ee118d4"),
            Code = "NL",
            Title = "Новые люди",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("d042a040-8020-406c-91db-944e0efa5f74"),
            Code = "INDEPENDENT",
            Title = "Вне фракций",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("e97addca-c253-4ff8-b3eb-2e6a41491221"),
            Code = "APR",
            Title = "АПР",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("81264f05-daf0-4b91-9ba8-381f31404362"),
            Code = "VR",
            Title = "ВР",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("c87fb255-1817-441e-a700-406fc2b6df61"),
            Code = "DPR",
            Title = "ДПР",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("ebb9199b-c439-4c62-bc71-72eab85f2019"),
            Code = "PRES",
            Title = "ПРЕС",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("df00511a-7978-4289-8fb8-da47a4ef80b1"),
            Code = "ZHR",
            Title = "ЖР",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("68fe8a83-5d8b-4b26-8312-1957075bde79"),
            Code = "YABLOKO",
            Title = "ЯБЛ",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("56dace41-92af-49d2-b6c7-622b13bd4fbc"),
            Code = "STAB",
            Title = "Стаб",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("6d678469-8ac9-42b5-bffb-615937bc14fb"),
            Code = "ROSS",
            Title = "Росс",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("e768d23a-f9ee-4974-bd59-b667d0d49206"),
            Code = "N96",
            Title = "Н-96",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("58c90289-b686-4eea-9479-c605be93b21a"),
            Code = "LDS",
            Title = "ЛДС",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("ce75e7e7-78cc-4280-89f8-833d0346f7ac"),
            Code = "NDR",
            Title = "НДР",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("845e78af-e7f2-4bdb-a014-f63ca4eebfd8"),
            Code = "AGR",
            Title = "Агр",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("2669c56a-cff0-4135-95a6-1b4e1696674b"),
            Code = "NRDV",
            Title = "НРДВ",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("2a6e744b-7858-4e83-a3d8-7d1dc63a03ad"),
            Code = "RREG",
            Title = "РРег",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("904b8d63-2500-435c-87f1-3d5467989207"),
            Code = "SPS",
            Title = "СПС",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("315fdc5b-11ad-407e-a45d-1cb778bd822d"),
            Code = "APG",
            Title = "АПГ",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("d77c1470-3098-4e22-8f7e-29866bb6a976"),
            Code = "NDEP",
            Title = "НДеп",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("ceeb9b8d-12e6-469c-940f-fa04152e633e"),
            Code = "RROS",
            Title = "РРос",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
        new Faction()
        {
            Id = Guid.Parse("dd3b261e-94c8-4827-9855-2f0d345e0b64"),
            Code = "RNVS",
            Title = "РНВС",
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        },
    };

    /// <summary>
    /// Возвращает фиксированный идентификатор фракции по её коду
    /// </summary>
    /// <param name="code">Код канонической фракции (например, ER)</param>
    /// <returns>Идентификатор фракции или <see cref="Guid.Empty"/>, если код неизвестен</returns>
    public static Guid DefaultFactionToGuid(string code)
    {
        switch (code)
        {
            case "ER": return Guid.Parse("f0e605dc-c9c2-4894-8c83-13bf0480e62d");
            case "KPRF": return Guid.Parse("6699bf12-62aa-4157-8ce5-ba600cc9b3a1");
            case "LDPR": return Guid.Parse("b5265678-a153-4ade-9010-022275aabac6");
            case "SR": return Guid.Parse("51cf331e-c029-4e16-9eec-affba832201e");
            case "NL": return Guid.Parse("92d7276d-bea2-4751-a4c1-7f8c0ee118d4");
            case "INDEPENDENT": return Guid.Parse("d042a040-8020-406c-91db-944e0efa5f74");
            case "APR": return Guid.Parse("e97addca-c253-4ff8-b3eb-2e6a41491221");
            case "VR": return Guid.Parse("81264f05-daf0-4b91-9ba8-381f31404362");
            case "DPR": return Guid.Parse("c87fb255-1817-441e-a700-406fc2b6df61");
            case "PRES": return Guid.Parse("ebb9199b-c439-4c62-bc71-72eab85f2019");
            case "ZHR": return Guid.Parse("df00511a-7978-4289-8fb8-da47a4ef80b1");
            case "YABLOKO": return Guid.Parse("68fe8a83-5d8b-4b26-8312-1957075bde79");
            case "STAB": return Guid.Parse("56dace41-92af-49d2-b6c7-622b13bd4fbc");
            case "ROSS": return Guid.Parse("6d678469-8ac9-42b5-bffb-615937bc14fb");
            case "N96": return Guid.Parse("e768d23a-f9ee-4974-bd59-b667d0d49206");
            case "LDS": return Guid.Parse("58c90289-b686-4eea-9479-c605be93b21a");
            case "NDR": return Guid.Parse("ce75e7e7-78cc-4280-89f8-833d0346f7ac");
            case "AGR": return Guid.Parse("845e78af-e7f2-4bdb-a014-f63ca4eebfd8");
            case "NRDV": return Guid.Parse("2669c56a-cff0-4135-95a6-1b4e1696674b");
            case "RREG": return Guid.Parse("2a6e744b-7858-4e83-a3d8-7d1dc63a03ad");
            case "SPS": return Guid.Parse("904b8d63-2500-435c-87f1-3d5467989207");
            case "APG": return Guid.Parse("315fdc5b-11ad-407e-a45d-1cb778bd822d");
            case "NDEP": return Guid.Parse("d77c1470-3098-4e22-8f7e-29866bb6a976");
            case "RROS": return Guid.Parse("ceeb9b8d-12e6-469c-940f-fa04152e633e");
            case "RNVS": return Guid.Parse("dd3b261e-94c8-4827-9855-2f0d345e0b64");
            default: return Guid.Empty;
        }
    }
}