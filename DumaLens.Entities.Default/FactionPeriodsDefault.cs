using DumaLens.Entities;

namespace DumaLens.Entities.Default;

/// <summary>
/// Стандартные исторические периоды фракций
/// </summary>
/// <remarks>
/// Состав — по наблюдениям паспорта данных (02-data-sources.md, §2.3.6); полный справочник достраивается импортом.
/// «Е-ЕР»/«О-ЕР» (III созыв) отнесены к канонической «Единой России» как её предшественники (паспорт, §2.3.6).
/// Даты периода совпадают с фактическими границами созыва; идентификаторы фиксированы для HasData.
/// </remarks>
public class FactionPeriodsDefault
{
    /// <summary>
    /// Сущности стандартных периодов фракций
    /// </summary>
    public static List<FactionPeriod> Entities = new List<FactionPeriod>()
    {
        CreatePeriod("b12d0021-afb4-4d9c-afb4-8ecc1520be9b", "KPRF", 1, 72100004, "КПРФ"),
        CreatePeriod("e7125771-2a99-4ff2-9fd1-afc68c841c87", "KPRF", 2, 72100004, "КПРФ"),
        CreatePeriod("6c55d2de-8184-4847-a937-7a5604375e98", "KPRF", 3, 72100004, "КПРФ"),
        CreatePeriod("9dc5165f-df1e-4574-85c5-b4f6f827500c", "KPRF", 4, 72100004, "КПРФ"),
        CreatePeriod("0a3c565b-ad02-447c-a9be-9eb10f70c354", "KPRF", 5, 72100004, "КПРФ"),
        CreatePeriod("45fc0012-037f-4ef7-86b2-e5373b73e60e", "KPRF", 6, 72100004, "КПРФ"),
        CreatePeriod("9eca1ed4-0320-4122-8dff-06ecd774f433", "KPRF", 7, 72100004, "КПРФ"),
        CreatePeriod("3f1e4c94-4a1b-4b5c-9db6-0a095b12d960", "KPRF", 8, 72100004, "КПРФ"),
        CreatePeriod("ad275bd5-0e8a-404e-91b8-bce6313aa5f4", "LDPR", 1, 72100005, "ЛДПР"),
        CreatePeriod("b562bf61-a5ea-4b29-807f-9f9fb68bfd5c", "LDPR", 2, 72100005, "ЛДПР"),
        CreatePeriod("ca43b4b7-32eb-4041-af2c-ac5c3fb6fea1", "LDPR", 3, 72100005, "ЛДПР"),
        CreatePeriod("78fbf9e3-d315-4c9e-b71e-509a58ff7e13", "LDPR", 4, 72100005, "ЛДПР"),
        CreatePeriod("73aa83b0-4cbd-4d1d-bfd3-817b7584691f", "LDPR", 5, 72100005, "ЛДПР"),
        CreatePeriod("e4d51638-bdeb-4587-8eef-7a9acb92f84a", "LDPR", 6, 72100005, "ЛДПР"),
        CreatePeriod("ee3550f1-a11b-4d54-b1f7-f2f763bf75c4", "LDPR", 7, 72100005, "ЛДПР"),
        CreatePeriod("b949ed05-f648-4847-b220-b24f5f2fbbf7", "LDPR", 8, 72100005, "ЛДПР"),
        CreatePeriod("1782447e-910d-42f9-ab92-cf47dfffd09e", "INDEPENDENT", 1, 72100011, "Вне фракций"),
        CreatePeriod("525de271-d6b5-4cc6-97c9-68160fe063a6", "INDEPENDENT", 2, 72100011, "Вне фракций"),
        CreatePeriod("e1c57b62-84e1-4ca4-b377-daa0379cb03f", "INDEPENDENT", 3, 72100011, "Вне фракций"),
        CreatePeriod("1a15de28-511d-4807-9d47-361ae282559a", "INDEPENDENT", 4, 72100011, "Вне фракций"),
        CreatePeriod("de5885e3-2f98-40f7-a81a-5d59ab86c3ea", "INDEPENDENT", 7, 72100011, "Вне фракций"),
        CreatePeriod("2e323136-978d-4ae6-bded-ae56b10c95c4", "INDEPENDENT", 8, 72100011, "Вне фракций"),
        CreatePeriod("eb123a42-74fa-46d6-9cd9-93b39bbfc5af", "ER", 4, 72100024, "ЕР"),
        CreatePeriod("9c8ce4cd-cfaa-45b5-b78f-e1d403bcfeed", "ER", 5, 72100024, "ЕР"),
        CreatePeriod("097aeacc-55c4-4c00-a595-daf65eed644b", "ER", 6, 72100024, "ЕР"),
        CreatePeriod("9fad73c0-322a-4b9a-813f-b33163b6f66b", "ER", 7, 72100024, "ЕР"),
        CreatePeriod("3064d9e6-d9fe-46b7-9878-466b1cb70f1d", "ER", 8, 72100024, "ЕР"),
        CreatePeriod("95596282-12fd-4831-b70d-2d04c15c5a95", "ER", 3, 72100009, "Е-ЕР"),
        CreatePeriod("a583d916-1cca-4532-a7e0-a217b8e854e3", "ER", 3, 72100010, "О-ЕР"),
        CreatePeriod("92db6383-260b-42d5-b7d2-bb3d55fd3a87", "SR", 4, 72100025, "СР-Р"),
        CreatePeriod("e326e66d-30cf-4e9d-a0ce-cff4c9b258c0", "SR", 5, 72100027, "СР"),
        CreatePeriod("64845a76-f80a-495c-9b7f-2d316b8b08e1", "SR", 6, 72100027, "СР"),
        CreatePeriod("b51c99bb-7967-4ea0-a9cf-7f9f2eefa604", "SR", 7, 72100027, "СР"),
        CreatePeriod("03b2975f-0c6d-4117-ae9c-6437713e4cf7", "SR", 8, 72100029, "СР"),
        CreatePeriod("360aef12-a5e7-4876-8080-791474fe2683", "NL", 8, 72100028, "НЛ"),
        CreatePeriod("ce2fc4c1-b08f-464b-a6db-afc84747133a", "APR", 1, 72100001, "АПР"),
        CreatePeriod("18edbd04-2265-457f-9e8b-0a17377a26aa", "VR", 1, 72100002, "ВР"),
        CreatePeriod("ac681593-4b27-4736-830b-0485a902f652", "DPR", 1, 72100003, "ДПР"),
        CreatePeriod("bdf9bb0b-7949-437b-b984-1eac067eaef7", "PRES", 1, 72100006, "ПРЕС"),
        CreatePeriod("ee81b4a1-6a69-436d-a48b-6d7d328c5c2b", "ZHR", 1, 72100007, "ЖР"),
        CreatePeriod("ff959403-b47e-4f60-bff9-a9d3e24f00d1", "YABLOKO", 1, 72100008, "ЯБЛ"),
        CreatePeriod("fd3ba9c6-d7d9-4381-b955-80b7ab854d59", "YABLOKO", 2, 72100008, "ЯБЛ"),
        CreatePeriod("e6658cd3-6592-4872-b89c-548a00f7ed67", "YABLOKO", 3, 72100008, "ЯБЛ"),
        CreatePeriod("3d299bfd-86ab-40aa-a7a3-feb41ed35835", "STAB", 1, 72100012, "Стаб"),
        CreatePeriod("0d61b8d3-3374-49e9-8dec-fe9d887ee1e2", "ROSS", 1, 72100013, "Росс"),
        CreatePeriod("83b180c6-e062-4043-92fc-4e195fff8ae2", "N96", 1, 72100014, "Н-96"),
        CreatePeriod("3e57a969-8b4c-4bc0-b780-4164d8fdfafb", "LDS", 1, 72100016, "ЛДС"),
        CreatePeriod("de7587b9-4c9b-4841-83fb-0a0ca064c790", "NDR", 2, 72100017, "НДР"),
        CreatePeriod("b3140166-5362-42fc-97cb-69766b9cf021", "AGR", 2, 72100018, "Агр"),
        CreatePeriod("479075db-b7f5-459f-9009-588d946fb4d1", "NRDV", 2, 72100019, "НРДВ"),
        CreatePeriod("ff5fdbfd-f7c9-401e-a489-a4d844c49050", "RREG", 2, 72100020, "РРег"),
        CreatePeriod("13d93636-ebe0-48c1-8caf-a1644038f060", "SPS", 3, 72100015, "СПС"),
        CreatePeriod("037fba2c-4821-4f0c-b118-191702aa102b", "APG", 3, 72100021, "АПГ"),
        CreatePeriod("57b2cb7c-c086-4b0a-a651-1d25d2e8bf51", "NDEP", 3, 72100022, "НДеп"),
        CreatePeriod("df045fa7-b9cb-4edc-8e8f-b3632dc638c3", "RROS", 3, 72100023, "РРос"),
        CreatePeriod("2da0ebe0-a68b-4a57-9506-a766548db824", "RNVS", 4, 72100026, "РНВС"),
    };

    /// <summary>
    /// Создаёт период фракции с датами соответствующего созыва
    /// </summary>
    /// <param name="id">Фиксированный идентификатор периода</param>
    /// <param name="factionCode">Код канонической фракции</param>
    /// <param name="convocationNumber">Номер созыва</param>
    /// <param name="siteId">Идентификатор фракции на сайте голосований</param>
    /// <param name="title">Название фракции в этом созыве</param>
    /// <returns>Период фракции</returns>
    private static FactionPeriod CreatePeriod(string id, string factionCode, int convocationNumber, long siteId, string title)
    {
        var convocation = ConvocationsDefault.Entities.First(x => x.Number == convocationNumber);
        return new FactionPeriod()
        {
            Id = Guid.Parse(id),
            FactionId = FactionsDefault.DefaultFactionToGuid(factionCode),
            ConvocationId = convocation.Id,
            SiteId = siteId,
            Title = title,
            DateFrom = convocation.DateFrom,
            DateTo = convocation.DateTo,
            CreatedAt = SeedTimestamp.Value,
            UpdatedAt = SeedTimestamp.Value,
        };
    }
}